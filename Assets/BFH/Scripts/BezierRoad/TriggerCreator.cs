/// Helper class for the creation of trigger colliders
/// 
/// ///

using UnityEngine;

public class TriggerCreator
{
    private GameObject triggerContainer;
    private int index;

    public TriggerCreator(int idx)
    {
        index = idx;
    }

    /// <summary>
    /// Create trigger collider for each of the segments.
    /// Currently, 1 type of trigger collider:
    /// - cam road trigger
    /// </summary>
    /// <param name="state"></param>
    public void CreateTriggerColliders(BezierRoadState state)
    {
        // copy state variable references
        Vector3[] leftPoints = state.bezierCurves[index].GetLeftPoints();
        Vector3[] rightPoints = state.bezierCurves[index].GetRightPoints();
        // int trigPerS = state.triggersPerSegment;
        Transform parentTransform = state.roadMeshes[index].transform;

        Transform existing = parentTransform.Find("TriggerContainer");

        if (existing != null)
        {
            triggerContainer = existing.gameObject;

            // delete previous triggers
            for (int i = triggerContainer.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(triggerContainer.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            // create 
            triggerContainer = new GameObject("TriggerContainer");
            triggerContainer.transform.SetParent(parentTransform, false);
        }

        // make sure trigger container has the trigger script component (only add if missing)
        var triggerScriptCam = triggerContainer.GetComponent<CamRoadTrigger>();

        if (triggerScriptCam == null)
        {
            triggerScriptCam = triggerContainer.AddComponent<CamRoadTrigger>();

        }


        // // determine whether this is the last segment
        // bool isLast = index == state.segmentCount - 1;
        Vector3 heightOffset = new Vector3(0, 10f, 0);


        int trigIdx = index;
        int arrayIdx = leftPoints.Length - 4;
        triggerScriptCam.CreateTrigger(leftPoints[arrayIdx], rightPoints[arrayIdx], heightOffset, trigIdx, $"CamTrigger{trigIdx}");
        

    }

}