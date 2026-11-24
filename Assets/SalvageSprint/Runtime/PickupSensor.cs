using System;
using System.Collections.Generic;
using SalvageSprint.Core;
using UnityEngine;

namespace SalvageSprint
{
    public sealed class PickupSensor : MonoBehaviour
    {
        [SerializeField] private MatchController match;
        [SerializeField, Min(1)] private float radius = 10;
        public float Radius => radius;
        public void Configure(MatchController owner) { match = owner; }

        public Observation Capture(RaceActor actor, int rememberedId, Func<int, bool> blocked)
        {


            var candidates = new List<Candidate>();
            foreach (SalvagePickup item in match.Pickups)
            {
                if (!match.IsAvailable(item.Id)) continue;
                float distance = MatchController.FlatDistanceSquared(transform.position, item.transform.position);


                bool remembered = item.Id == rememberedId;
                bool inRange = distance <= radius * radius;

                if (!inRange && !remembered) continue;

                // If the candidate scrap is within range of the sensor and a linecast can be drawn to it without being blocked
                // by sight blockers then it is a viable candidate.
                bool visible = inRange && !Physics.Linecast(transform.position + Vector3.up,
                    item.transform.position + Vector3.up * .6f,
                    match.SightBlockers, QueryTriggerInteraction.Ignore);

                // Broad-phase candidates are data to process, not automatically known targets.
                // Add in all candidates and their Id, distance, true(???), if they're visible or not from the rival, if the rival
                // remembers the candidate and if the rival already knows if the candidate is blocked or not.
                candidates.Add(new Candidate(item.Id, distance, true, visible, remembered, blocked(item.Id)));
            }
            // return if we are carrying something, if we have a current candidate we are going for
            //  and the candidates found in the observation
            return new Observation(actor.Carrying, rememberedId, candidates.ToArray());
        }

        private void OnDrawGizmosSelected()
        { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, radius); }
    }
}
