namespace SalvageSprint.Core
{
    // Week 1 assignment starter. Work through README stages in this same project.
    // STAGE 2: distinguish visible candidates from unknown hidden objects.
    // STAGE 3: retain a usable remembered target. STAGE 4: use failed-route feedback.
    // Delivery is supplied so the unmodified game has a complete scoring loop.
    public static class DecisionPolicy
    {
        public const string Label = "Part 1 starter: nearest candidate";

        public static AgentFacts Infer(Observation observation)
        {
            if (observation.Carrying) return new AgentFacts(true, -1, false);

            // if the candidate is remembered aka it's the one the rival is going for then dont go for the nearest candidate
            foreach (Candidate item in observation.Candidates)
            {
                if (item.Remembered && item.Available && !item.Blocked && (item.Visible || item.Remembered)) return new AgentFacts(false, item.Id, true);
            }

            int nearest = -1;
            float distance = float.PositiveInfinity;
            
            foreach (Candidate candidate in observation.Candidates)
            {
                bool known = candidate.Available && !candidate.Blocked && (candidate.Visible || candidate.Remembered);
                if (!known) continue;
                // Deliberately naive: ignores Visible, Remembered and Blocked.
                // Using this, the rival would attempt to move towards a candidate that is not reachable without the known bool.
                if (candidate.Available && candidate.DistanceSquared < distance)
                { 
                    nearest = candidate.Id; 
                    distance = candidate.DistanceSquared; 
                }
            }
            return new AgentFacts(false, nearest, false);
        }

        public static Decision Choose(Observation observation) => Choose(Infer(observation));

        public static Decision Choose(AgentFacts facts)
        {
            if (facts.NeedsDelivery) return new Decision(TaskKind.Deliver, -1, "Cargo held: return to depot");

            return facts.HasUsableTarget
                ? new Decision(TaskKind.Collect, facts.SelectedTargetId, facts.RetainedTarget ? "Ignoring nearer candidates, has remembered" :"Nearest candidate; reselected each tick")
                : new Decision(TaskKind.Explore, -1, "No candidate: visit the next survey point");
        }
    }
}
