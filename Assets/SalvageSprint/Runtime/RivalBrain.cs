using System.Collections.Generic;
using SalvageSprint.Core;
using UnityEngine;

namespace SalvageSprint
{
    [RequireComponent(typeof(RaceActor), typeof(Navigator), typeof(PickupSensor))]
    public sealed class RivalBrain : MonoBehaviour
    {
        [SerializeField] private MatchController match;
        [SerializeField] private Transform[] surveyPoints;
        [SerializeField, Min(.05f)] private float decisionInterval = .15f;
        [SerializeField] private bool logDecisionChanges;
        private readonly Dictionary<int, float> failedUntil = new Dictionary<int, float>();
        private RaceActor actor;
        private Navigator navigator;
        private PickupSensor sensor;
        private int rememberedId = -1, surveyIndex;
        private float nextDecision;
        public Decision CurrentDecision { get; private set; }
        public Observation LastObservation { get; private set; }
        public AgentFacts CurrentFacts { get; private set; }
        public TravelStatus Travel => navigator != null ? navigator.Status : TravelStatus.Idle;
        public int DecisionCount { get; private set; }
        public int DestinationRequests => navigator != null ? navigator.DestinationRequests : 0;
        public string LastFailure { get; private set; } = "None";

        public void Configure(MatchController owner, Transform[] points)
        { match = owner; surveyPoints = points; }

        private void Awake()
        {
            actor = GetComponent<RaceActor>();
            navigator = GetComponent<Navigator>();
            sensor = GetComponent<PickupSensor>();
            CurrentDecision = new Decision(TaskKind.Explore, -1, "Waiting for first observation");
        }

        private void Update()
        {
            if (!match.IsRunning) { navigator.Cancel(); return; }
            TravelStatus travel = navigator.Poll();
            if (Time.time < nextDecision) return;

            nextDecision = Time.time + decisionInterval;

            // If we are trying to get to a ID that we already recognised as failed previously then give dome debug info.
            if (travel == TravelStatus.Failed && rememberedId >= 0)
            {
                failedUntil[rememberedId] = Time.time + 6;
                LastFailure = "Route failed for pickup " + rememberedId + "; ignore for 6 seconds";
            }
            
            // IF the remmebered Id no longer exists then reset it to -1
            if (rememberedId >= 0 && !match.IsAvailable(rememberedId)) rememberedId = -1;


            // walk between survey points in a loop.
            if (CurrentDecision.Task == TaskKind.Explore && (travel == TravelStatus.Arrived || travel == TravelStatus.Failed))
                surveyIndex = (surveyIndex + 1) % surveyPoints.Length;

            // Isblocked sends over the function to the sensor.
            LastObservation = sensor.Capture(actor, rememberedId, IsBlocked);
            CurrentFacts = DecisionPolicy.Infer(LastObservation);
            Decision next = DecisionPolicy.Choose(CurrentFacts);

            // If the task has changed then make a bool true to debug log it later and blah blah
            bool changed = next.Task != CurrentDecision.Task || next.TargetId != CurrentDecision.TargetId;
            CurrentDecision = next;
            DecisionCount++;
            if (changed && logDecisionChanges) Debug.Log($"[{Time.time:F1}] {next.Task} target={next.TargetId}: {next.Reason}", this);

            // If the rival has scrap then return it to depot has highest priority
            if (next.Task == TaskKind.Deliver)
            {
                rememberedId = -1;
                navigator.Request(-2, actor.DepotPosition);
            }
            // Otherwise if the rival has tasks pending aka scrap that they think they can get to then 
            else if (next.Task == TaskKind.Collect)
            {
                rememberedId = next.TargetId;
                SalvagePickup target = match.GetPickup(next.TargetId);
                
                if (target != null && match.IsAvailable(next.TargetId))
                    navigator.Request(next.TargetId, target.transform.position);
                else navigator.Cancel();
            }
            // if the rival has no scrap in the vicinity then roam around in set points in a loop until a scrap is found.
            else
            {
                rememberedId = -1;
                navigator.Request(-100 - surveyIndex, surveyPoints[surveyIndex].position);
            }
        }

        private bool IsBlocked(int id) => failedUntil.TryGetValue(id, out float until) && Time.time < until;
        private void OnDisable() { if (navigator != null) navigator.Cancel(); }
    }
}
