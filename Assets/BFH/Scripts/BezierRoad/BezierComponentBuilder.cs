/// Helper class to build and setup the Script Components and Game Objects of the Bezier Road
///

using UnityEngine;
using System.Linq;

public class BezierComponentBuilder
{
    /// <summary>
    /// Creates and sets up the components and game objects for the bezier Road.
    /// </summary>
    /// <param name="state">Bzier Road State</param>
    public static void AssignComponents(BezierRoadState state)
    {
        // copy state
        int segmentCount = state.segmentCount;
        // prevent re-creation by checking for existing child objects
        if (state.transform.childCount >= segmentCount)
        {
            state.bezierCurves = state.manager.GetComponentsInChildren<BezierCurveGroup>();
            state.roadMeshes = state.manager.GetComponentsInChildren<BezierRoadMesh>();
            state.bezierCurveObjects = state.bezierCurves.Select(c => c.gameObject).ToArray();
            state.roadMeshObjects = state.roadMeshes.Select(m => m.gameObject).ToArray();
            state.roadSegments = new GameObject[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                state.roadSegments[i] = state.bezierCurveObjects[i].transform.parent.gameObject;
            }
            return; // already created
        }

        // initate all arrays
        state.bezierCurves = new BezierCurveGroup[segmentCount];
        state.roadMeshes = new BezierRoadMesh[segmentCount];
        state.bezierCurveObjects = new GameObject[segmentCount];
        state.roadMeshObjects = new GameObject[segmentCount];
        state.roadSegments = new GameObject[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            // check if the objects already exist, if not, create them
            if (state.roadSegments[i] == null)
            {
                state.roadSegments[i] = new GameObject($"RoadSegment{i}");
                state.roadSegments[i].transform.parent = state.transform;
            }

            if (state.bezierCurveObjects[i] == null)
            {
                state.bezierCurveObjects[i] = new GameObject("BezierCurveContainer");
                state.bezierCurveObjects[i].transform.parent = state.roadSegments[i].transform; // set as a child of the manager
                state.bezierCurves[i] = state.bezierCurveObjects[i].AddComponent<BezierCurveGroup>();
            }
            else
            {
                state.bezierCurves[i] = state.bezierCurveObjects[i].GetComponent<BezierCurveGroup>();
            }

            if (state.roadMeshObjects[i] == null)
            {
                state.roadMeshObjects[i] = new GameObject("RoadMeshContainer");
                state.roadMeshObjects[i].transform.parent = state.roadSegments[i].transform; // set as a child of the manager
                state.roadMeshes[i] = state.roadMeshObjects[i].AddComponent<BezierRoadMesh>();
            }
            else
            {
                state.roadMeshes[i] = state.roadMeshObjects[i].GetComponent<BezierRoadMesh>();
            }
        }

    }

}