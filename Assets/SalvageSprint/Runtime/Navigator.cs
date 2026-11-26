using SalvageSprint.Core;
using UnityEngine;
using UnityEngine.AI;

namespace SalvageSprint
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Navigator : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float failedRequestRetryDelay = 1f;
        
        [SerializeField]
        private bool logNavigationFailures = true;
        
        // Visible in the Inspector, including failures during delivery.
        [SerializeField, TextArea]
        private string lastFailure = "None";
        
        private NavMeshAgent agent;
        private bool requested;
        private int goalKey = int.MinValue, requestFrame;
        private Vector3 goal, lastProgressPosition;
        private float lastProgressTime, retryAt;
        public TravelStatus Status { get; private set; } = TravelStatus.Idle;
        public Vector3 Goal => goal;
        public int DestinationRequests { get; private set; }

        private void Awake() { agent = GetComponent<NavMeshAgent>(); }

        public void Request(int key, Vector3 destination)
        {
            bool sameGoal = requested && key == goalKey && (destination - goal).sqrMagnitude < .01f;
            if (sameGoal && (Status != TravelStatus.Failed || Time.time < retryAt))
            {
                return;
            }
            
            requested = true;
            goalKey = key;
            goal = destination;
            requestFrame = Time.frameCount;

            lastProgressTime = Time.time;
            lastProgressPosition = transform.position;

            if (!CanNavigate())
            {
                Fail("Agent unavailable or not on a NavMesh");
                return;
            }

            agent.isStopped = false;
            DestinationRequests++;

            // If the destination is not null then set the travelstatus to pending.
            if (agent.SetDestination(destination))
            {
                Status = TravelStatus.Pending;
            }
            else
            {
                Fail("SetDestination rejected the request");
            }
        }

        public TravelStatus Poll()
        {
            if (!requested || Status == TravelStatus.Failed) return Status;
            if (Time.frameCount == requestFrame) return Status;

            if (agent == null || !agent.isOnNavMesh)
            { 
                Status = TravelStatus.Failed; 
                return Status; 
            }

            Status = NavigationFeedback.Classify(agent.isOnNavMesh, agent.pathPending,
                agent.pathStatus == NavMeshPathStatus.PathComplete, agent.hasPath,
                agent.remainingDistance, agent.stoppingDistance + .12f);

            if ((transform.position - lastProgressPosition).sqrMagnitude > .04f)
            { 
                lastProgressPosition = transform.position; 
                lastProgressTime = Time.time; 
            }

            if ((Status == TravelStatus.Moving || Status == TravelStatus.Pending) && Time.time - lastProgressTime > 5) 
            {
                Status = TravelStatus.Failed;
            }

            if (Status == TravelStatus.Failed && agent.isOnNavMesh)
            { 
                agent.isStopped = true; 
                var p = new NavMeshPath();
                bool got = NavMesh.CalculatePath(transform.position, goal, NavMesh.AllAreas, p);
                bool agentOnMesh = NavMesh.SamplePosition(transform.position, out var aHit, 0.5f, NavMesh.AllAreas);
                bool goalOnMesh = NavMesh.SamplePosition(goal, out var gHit, 2f, NavMesh.AllAreas);
                Debug.Log($"Nav fail: calc={got} status={p.status} agentOnMesh={agentOnMesh} " +
                        $"goalOnMesh={goalOnMesh} agentPos={transform.position} snappedGoal={gHit.position}", this);
                agent.ResetPath(); 
            }


            return Status;
        }

        public void Cancel()
        {
            if (!requested) return;
            if (agent != null && agent.isOnNavMesh)
            { 
                agent.isStopped = true; 
                agent.ResetPath(); 
            }
            requested = false;
            goalKey = int.MinValue;
            Status = TravelStatus.Idle;
        }
        private bool CanNavigate()
        {
            return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
        }

        private void Fail(string reason)
        {
            bool onMesh = CanNavigate();

            // Capture the failure evidence BEFORE ResetPath clears it.
            lastFailure =
                $"{reason}; key={goalKey}; goal={goal}"
                + $"; position={transform.position}"
                + $"; onMesh={onMesh}"
                + $"; noProgressFor={Time.time - lastProgressTime:F2}s";

            // Only inspect path properties when the agent can navigate.
            if (onMesh)
            {
                lastFailure +=
                    $"; pending={agent.pathPending}"
                    + $"; hasPath={agent.hasPath}"
                    + $"; pathStatus={agent.pathStatus}"
                    + $"; remainingDistance={agent.remainingDistance}"
                    + $"; stoppingDistance={agent.stoppingDistance}"
                    + $"; isStopped={agent.isStopped}";
            }

            Status = TravelStatus.Failed;
            retryAt = Time.time + failedRequestRetryDelay;

            if (onMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            // This also reports delivery failures, which the brain's
            // existing pickup-only failure message does not cover.
            if (logNavigationFailures)  Debug.LogWarning("[Navigator] " + lastFailure, this);
        }

        private void OnDrawGizmosSelected()
        {
            NavMeshAgent navigation = GetComponent<NavMeshAgent>();
            if (navigation == null || !navigation.isOnNavMesh || !navigation.hasPath) return;
            
            Vector3[] corners = navigation.path.corners;
            Gizmos.color = Color.yellow;

            for (int i = 1; i < corners.Length; i++)
                Gizmos.DrawLine(corners[i - 1] + Vector3.up * .15f, corners[i] + Vector3.up * .15f);
        }
    }
}
