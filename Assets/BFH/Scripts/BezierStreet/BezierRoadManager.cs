// Manages the creation of a Road System made up by road segments created with Bezier curves.
// The road system is composed of multiple tripplets of approximately equidistant cubic Bezier Curves, and their
// respective generated road mesh.
//
// TODO:
// - creation of multiple connected road meshes
// 
// Author: Ammar Hammad

using UnityEngine;

[ExecuteInEditMode()]
public class BezierRoadManager : MonoBehaviour
{
    private int segmentCount = 1;
    private BezierCurveExample[] bezierCurves;
    private BezierRoadMesh[] roadMeshes;
    private GameObject[] bezierCurveObjects;  // GObj for curves and lanes data
    private GameObject[] roadMeshObjects;  // GObj for road mesh
    private GameObject[] roadSegments;  // GObj for road segment


    void Start()
    {
        // create BezierCurveExample and BezierRoadMesh dynamically
        AssignComponents();

        // generate the initial road mesh
        UpdateRoadMesh();
    }

    void AssignComponents()
    {
        // initate all arrays
        bezierCurves = new BezierCurveExample[segmentCount];
        roadMeshes = new BezierRoadMesh[segmentCount];
        bezierCurveObjects = new GameObject[segmentCount];
        roadMeshObjects = new GameObject[segmentCount];
        roadSegments = new GameObject[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            // check if the objects already exist, if not, create them
            if (roadSegments[i] == null)
            {
                roadSegments[i]  = new GameObject("RoadSegment");
                roadSegments[i].transform.parent = this.transform;
            }

            if (bezierCurveObjects[i] == null)
            {
                bezierCurveObjects[i] = new GameObject("BezierCurveContainer");
                bezierCurveObjects[i].transform.parent = roadSegments[i].transform; // set as a child of the manager
                bezierCurves[i] = bezierCurveObjects[i].AddComponent<BezierCurveExample>();
            }
            else
            {
                bezierCurves[i] = bezierCurveObjects[i].GetComponent<BezierCurveExample>();
            }

            if (roadMeshObjects[i] == null)
            {
                roadMeshObjects[i] = new GameObject("RoadMeshContainer");
                roadMeshObjects[i].transform.parent = roadSegments[i].transform; // set as a child of the manager
                roadMeshes[i] = roadMeshObjects[i].AddComponent<BezierRoadMesh>();
            }
            else
            {
                roadMeshes[i] = roadMeshObjects[i].GetComponent<BezierRoadMesh>();
            }
        }

    }

    void Update()
    {
        // Update road mesh dynamically (if curves change)
        UpdateRoadMesh();
    }

    private void UpdateRoadMesh()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            roadMeshes[i].GenerateRoadMesh(bezierCurves[i].GetLeftPoints(), bezierCurves[i].GetRightPoints());
        }
    }
}

