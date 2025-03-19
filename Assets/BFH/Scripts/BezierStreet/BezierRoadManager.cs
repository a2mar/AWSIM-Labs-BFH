// Manages the creation of a Road System made up by road segments created with Bezier curves.
// The road system is composed of multiple tripplets of approximately equidistant cubic Bezier Curves, and their
// respective generated road mesh.
//
// TODO:
// - don't call Start on Play 
// - add randomization to road segments
//
// 
// Author: Ammar Hammad

using System;
using UnityEngine;

[ExecuteInEditMode()]
public class BezierRoadManager : MonoBehaviour
{
    private int segmentCount = 8;  // at least 4 for cricle creation
    private float radius = 160.0f;
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
            Vector3[] previous = i > 0 ? bezierKnots[i - 1] : null;
            // bezierKnots[i] = CubicBezierKnots(previous);
            bezierKnots[i] = CircularCubicBezierKnots(i);
            // apply to the corresponding Bezier Curve Components
            bezierCurves[i].ApplyMainBezierKnots(bezierKnots[i]);
        }
    }

    /// <summary>
    /// TODO: use the previous Bezier Curve (if there is any) as parameter to calculate the next cubic Bezier Curve.
    /// use the previous segment's orientation for the rotation, and its last two control knots for continuity (C2, preferably)
    /// </summary>
    /// <returns></returns>
    Vector3[] CubicBezierKnots(Vector3[] previous = null)
    {
        if (previous.Length != 4)
        {
            Debug.LogError("Format mismatch: Knots array must consist of 4 vectors");
            return null;
        }
        // calculate the general direction of the previous curve
        Vector3 direction = previous[3] - previous[0];

        return new Vector3[] { new(0, 0, 0), new(10, 0, 20), new(15, 0, 30), new(4, 0, 40) };
    }

    /// <summary>
    /// Use the count of segments, the current segment number, and a sampled circle with a defined radius to create Bezier knots
    /// </summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    Vector3[] CircularCubicBezierKnots(int segment)
    {
        // check if minimum amount of segments is 4
        if (segmentCount < 4)
        {
            Debug.LogError("Two little segments for Cricle creation");
            return null;
        }
        // calculate angle between endpoints
        float angleRadian = (float)(Math.PI * 2 / segmentCount);

        // start points
        float x_0 = radius * Mathf.Cos(angleRadian * segment);
        float z_0 = radius * Mathf.Sin(angleRadian * segment);
        Vector3 p_0 = new(x_0, 0, z_0);

        // end points
        float x_3 = radius * Mathf.Cos(angleRadian * (segment + 1));
        float z_3 = radius * Mathf.Sin(angleRadian * (segment + 1));
        Vector3 p_3 = new(x_3, 0, z_3);

        // CALCULATE the intermediary knots
        // define normal vector
        Vector3 normal = new(0, 1, 0);
        // calculate the amplitude factor for the tangents. 
        // It is directly related to the number of segments for the circle.
        // from Stackoverflow: (4/3)*tan(pi/(2n)) (https://stackoverflow.com/questions/1734745/how-to-create-circle-with-b%C3%A9zier-curves)
        float amp = (4.0f / 3.0f) * Mathf.Tan( (float) (Math.PI / (2.0f * segmentCount) ) );
        
        //float amp = (float) (2.00f / segmentCount);  // primitive, inaccurate variant

        // calculate the endpoint of the start tangent, which is the first intermediary point
        Vector3 p_1 = amp * Vector3.Cross(p_0, normal) + p_0;
        // calculate the endpoint of the end tangent, which is the second intermediary point
        Vector3 p_2 = amp * Vector3.Cross(normal, p_3) + p_3;

        return new Vector3[] { p_0, p_1, p_2, p_3 };
        // return new Vector3[] { new(0, 0, 0), new(10, 0, 20), new(15, 0, 30), new(4, 0, 40) };
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
                roadSegments[i] = new GameObject($"RoadSegment{i}");
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

