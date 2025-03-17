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
    private BezierCurveExample[] bezierCurves;  // Bezier Curve Data component
    private BezierRoadMesh[] roadMeshes;  // road mesh component
    private GameObject[] bezierCurveObjects;  // GObj for curves and lanes data
    private GameObject[] roadMeshObjects;  // GObj for road mesh
    private GameObject[] roadSegments;  // GObj for road segment

    // Bezier Vectors [row][col], each row defines the BezierKnots for a cubic bezier curve.
    private Vector3[][] bezierKnots;  // maybe unused, when the Bezier knots are directly applied to the Bezier Curve Components

    void Start()
    {
        // create BezierCurveExample and BezierRoadMesh dynamically
        AssignComponents();
        // create Vectors for the Bezier curves and update the Bezier Curve Components
        InitializeBezierKnots();
        // generate the initial road mesh
        UpdateRoadMesh();
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

    void InitializeBezierKnots()
    {
        // allocate the Bezier Knots array
        bezierKnots = new Vector3[segmentCount][];

        // allocate each row to hold 4 Vector3 elements (cubic Bezier curve's control knots)
        for (int i = 0; i < segmentCount; i++)
        {
            bezierKnots[i] = CubicBezierKnots();
            // apply to the corresponding Bezier Curve Components
            bezierCurves[i].ApplyMainBezierKnots(bezierKnots[i]);
        }
    }

    /// <summary>
    /// TODO: use the previous Bezier Curve (if there is any) as parameter to calculate the next cubic Bezier Curve.
    /// use the previous segment's orientation for the rotation, and its last two control knots for continuity (C2, preferably)
    /// </summary>
    /// <returns></returns>
    Vector3[] CubicBezierKnots()
    {
        return new Vector3[] { new(0, 0, 0), new(10, 0, 20), new(15, 0, 30), new(4, 0, 40) };
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
                roadSegments[i] = new GameObject("RoadSegment");
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

}

