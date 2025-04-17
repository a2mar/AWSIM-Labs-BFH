using UnityEngine;

public class RoadTriggerListener : MonoBehaviour
{
    private RoadTrigger _roadTrigger;
    // private static int targetTriggerIndex;

    public void Initialize(RoadTrigger trigger)
    {
        _roadTrigger = trigger;
        // targetTriggerIndex = 4;  // set the target trigger index to 4 in the beginning to skip the first segment
    }

    private void OnTriggerEnter(Collider other)
    {
        // //Debug.Log($"TRIGGER LISTERER: the trigger called is: {_roadTrigger.GetIndex()}");
        // if ( _roadTrigger.GetIndex() == targetTriggerIndex)
        // {
        //     _roadTrigger?.OnRoadTriggerEnter(other.gameObject, gameObject, true);
        // }
        _roadTrigger?.OnRoadTriggerEnter(other.gameObject, gameObject, false);

    }

    private void Awake()
    {
        _roadTrigger = GetComponentInParent<RoadTrigger>();
        if (_roadTrigger == null)
            Debug.LogError("No RoadTrigger found in parent hierarchy!", this);
    }



}
