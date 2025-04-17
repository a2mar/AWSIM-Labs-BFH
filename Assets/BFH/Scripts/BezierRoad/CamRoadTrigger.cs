using UnityEngine;

public class CamRoadTrigger : RoadTrigger
{
    public override void OnRoadTriggerEnter(GameObject triggeringObject, GameObject triggerCatcher, bool positive)
    {
        Debug.Log($"[Cam Trigger] {triggerCatcher.name} car detected: {triggeringObject.name}");

        // Check the root object or look upward
        // CarAgent agent = triggeringObject.GetComponentInParent<CarAgent>();

        // if (agent != null)
        // {
        //     agent.GetStatsRecorder().Add("Collisions", 1, StatAggregationMethod.Sum);
        //     Debug.Log("Car Agent found and should be penalized");
        //     agent.AddReward(-4.0f); // penalty for leaving the road
        //     agent.EndEpisode();     // optional reset
        //     RoadTriggerListener.ResetTargetIndex();
        // }

    }
}