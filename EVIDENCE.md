# Week 1 evidence

Keep this short. Bullet points, a small table and one diagram or code trace are enough. This is your checkpoint record, not an extra report. Replace the prompts as you work.

Name: Zion Falk
Exact Unity version: 6000.3.26f1
Checkpoint commit / repository: https://github.com/imEonz/ZionFalk_AI_project
Current stage and anything unfinished: Stage 5

## Initial prediction

Three observations the rival needs, including one it might not have: 

    Which pickups exist and are still available.
    Which pickups it can currently see.
    Whether its current route is usable.

    Pickups behind the sight-blocking walls it has never seen, so it doesnt know where it is.

Seeing a pickup across a gap proves / does not prove reachability because: Only the NavMesh can decide if its reachable, like how in F6 it's visible but not reachable.

One question about NPC behaviour: How much memory should an AI try and use? Like in alien isolation I'd imagine a lot is used to remember the player's actions and movement.

## One complete trace

Use one actual situation. Include the observation data, derived facts, selected task and reason, movement request/result, and how feedback affects a later decision. Identify the responsible code locations. A tiny state diagram can accompany this trace.

Situation: F5, target taken. The rival is heading to a remembered pickup and the other agent collects it first.

Observation: MatchController marks the pickup unavailable when the other agent collects it.
Memory release: on the next decision, RivalBrain.Update runs if (rememberedId >= 0 && !match.IsAvailable(rememberedId)) rememberedId = -1;, so the old target is forgotten.
Sensing: PickupSensor.Capture is called with remembered id -1, which matches no pickup. It also skips items where !match.IsAvailable, so the stolen pickup is not a candidate.
Derived facts: DecisionPolicy.Infer finds no remembered candidate.
Selection: it picks the nearest known (visible/remembered) available candidate, or Explore if none exists.
Movement: Navigator requests a new route (SetDestination) only when the target changes. Poll and NavigationFeedback.Classify report pending / usable / arrived, which feeds later decisions.

Explore -> Collect: A candidate is found by the sensor that is visible.
Collect -> Collect The candidate is still remembered and the AI is still going for it.
Collect -> Deliver: The rival collected the scrap and needs to drop it off at the depot.
Deliver -> Explore: Deliver is finished and there is no known scrap nearby.
Deliver -> Collect: Deliver is finished and there is already a known scrap to collect.
Collect -> Explore, or Collect → Collect: Scrap was picked up by player so its forgotten and the selection happens again.

## Scenario record

| Scenario | Predicted result | Observed result and short evidence / remaining issue |
|---|---|---|
| Normal loop and restart | Collect, Deliver, Explore for more scrap repeat. | The rival collected all scrp near it succesfully, then proceeded to explore for more until it or the player won. |
| Carrying cargo | Deliver first then go for more scrap | It first delivered scrap, then collected other scrap. |
| Hidden / remembered pickup | Ignore the hidden pickup and explore after collecting the visible scrap |  |
| Target taken | Release the remembered scrap and continue | |
| Unreachable target | Pick another scrap to go for and send fail feedback | |
| No usable pickup | Continue to explore in a loop forever | |

Supplied test result, with any added tests identified: Salvage Sprint (22 tests) (0.011s)
Live walkthrough checked by / recording link if used: 

## One correction and one independent check

The original weakness and why it happened: nearest-candidate selection ignored perception and memory, so hidden unknown scrap could win just by being closer.
What I changed and why it addresses the cause: What I changed and why it addresses the cause: Infer now filters out unavailable and unknown hidden candidates before picking the nearest scrap.
My counterexample or experiment: prediction, observed result, and one limitation: I placed 2 pieces of scrap at an equal distance from the rival. I predicted that the AI would pick randomly between the two at start. Upon starting the experiment the AI instead consistenly went for the scrap 0 instead of scrap 1. After further testing I concluded that in the case of scrap with an equal distance, the AI picks the one first in the match pickups list.

## Collaboration and help

People I discussed or playtested with: No one currently
Code, sources or substantive tools/AI assistance I used, and how I verified them: When encoutering the bug with not being able to pathfind back to the depot when blocked by a wall I could not figure it out and used AI to try and find the issue, ultimately I didn't know the code good enough to even begin fixing that and it went nowhere.
If unfinished: what works, exact reproduction steps, and the help I need: Nothing so far.
