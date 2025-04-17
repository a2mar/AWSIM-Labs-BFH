using UnityEngine;

public abstract class RoadTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public float triggerThickness = 0.1f;
    public float triggerHeight = 1.0f;

    [SerializeField]
    private int index = -1;
    [SerializeField]
    private bool isLast = false;

    /// <summary>
    /// Creates a trigger collider between two points, elevated by a height offset.
    /// </summary>
    /// <param name="left">Left edge of the road or segment</param>
    /// <param name="right">Right edge of the road or segment</param>
    /// <param name="heightOffset">Vertical offset from the road surface</param>
    /// <param name="name">Optional name for the created trigger GameObject</param>
    public void CreateTrigger(Vector3 left, Vector3 right, Vector3 heightOffset, int idx, string name = "Trigger", bool last = false)
    {
        GameObject triggerObj = new GameObject(name);
        triggerObj.transform.parent = this.transform;

        Vector3 center = (left + right) * 0.5f + heightOffset * 0.5f;
        triggerObj.transform.position = center;

        BoxCollider trigger = triggerObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;

        float width = Vector3.Distance(left, right);
        float height = heightOffset.y;

        // Orient the collider to match the direction between the left and right points
        Vector3 direction = (right - left).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        triggerObj.transform.rotation = rotation;

        trigger.size = new Vector3(triggerThickness, height, width);

        // dynamically add the *same type* of RoadTrigger to the new trigger object
        RoadTrigger newTrigger = (RoadTrigger)triggerObj.AddComponent(this.GetType());

        newTrigger.SetIndex(idx);
        if (last) newTrigger.SetLastFlag(last);

        // register the listener to the new trigger
        var listener = triggerObj.AddComponent<RoadTriggerListener>();
        listener.Initialize(newTrigger);
    }


    private void SetIndex(int idx)
    {
        index = idx;
        //Debug.Log($"[SetIndex] Index set to: {index} on {gameObject.name}");
    }
    public int GetIndex() { return index; }

    private void SetLastFlag(bool flag)
    {
        isLast = flag;
    }

    public bool IsLast() => isLast;

    // Abstract method that children must implement to respond to trigger events
    public abstract void OnRoadTriggerEnter(GameObject triggeringObject, GameObject triggerCatcher, bool positive);
}