// Manages the creation of a Road System made up by road segments created with Bezier curves.
// The road system is composed of multiple tripplets of approximately equidistant cubic Bezier Curves, and their
// respective generated road mesh.
//
// TODO:
//
// 
// Author: Ammar Hammad

using System;
using UnityEngine;
using System.Linq;


// using System.Numerics;


#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode()]
public class BezierRoadManager : MonoBehaviour
{
    private int segmentCount = 32;  // at least 4 for cricle creation
    private float radius = 160.0f;
    private BezierCurveExample[] bezierCurves;  // Bezier Curve Data component
    private BezierRoadMesh[] roadMeshes;  // road mesh component
    private GameObject[] bezierCurveObjects;  // GObj for curves and lanes data
    private GameObject[] roadMeshObjects;  // GObj for road mesh
    private GameObject[] roadSegments;  // GObj for road segment

    // Bezier Vectors [row][col], each row defines the BezierKnots for a cubic bezier curve.
    private Vector3[][] bezierKnots;  // maybe unused, when the Bezier knots are directly applied to the Bezier Curve Components

    // how much the control knots can be randomply scattered
    [Header("Range of Random Scattering of Bezier Control Knots (0 means no random scattering)")]
    public float scatteringRange = 0f;
    // how likely it is that a certain knot is scatered
    [Header("Probability of random scattering per knot")]
    public float randomProb = 100.0f;

    // secondary scattering
    [Header("Range of Secondary Random Scattering of Bezier Control Knots (0 means no secondary random scattering)")]
    public float secundaryScatteringRange = 10f;
    private float[] randomNumbers;

    public enum RoadType
    {
        Simple,  // randomize every 8th Bezier end control knot
        Medium,  // randomize every 4th Bezier end control knot
        Crazy  //  randomize every 2nd Bezier end control knot
    }

    [Header("Road Type")]
    public RoadType roadType = RoadType.Simple;

    private int[] primaryScatterPoints;

    /// <summary>
    /// TODO: determine, in which mode the component is supposed to be started.
    /// </summary>
    void Awake()
    {
        if (Application.isPlaying)
        {
            return;  // Skip execution in Play Mode
        }
        // create BezierCurveExample and BezierRoadMesh dynamically
        AssignComponents();
        // create Vectors for the Bezier curves and update the Bezier Curve Components
        InitializeBezierKnots();
        // generate the initial road mesh
        UpdateRoadMesh();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                // Update the road, including random values, geometric and mesh components
                UpdateRoad();
            }
        };
#endif

    }

    void UpdateRoad()
    {
        // determin which Bezier segments to randomize with primary scattering, according to road type
        DetermineRandomBezierSegments();
        // generate deviation factors for random scattering
        GenerateRandomNumbers();
        // create Vectors for the Bezier curves and update the Bezier Curve Components
        InitializeBezierKnots();
        // primary random scattering of a fraction of Bezier segments 
        RandomizeBezierKnots();
        // adjust all other knots to the randomized knots, relaxing the curve, but add secondary random scattering
        AdjustKnotsWithScattering();

        // generate the initial road mesh
        UpdateRoadMesh();
    }
    public void UpdateRoadMesh()
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
            bezierKnots[i] = CircularCubicBezierKnotsV2(i);
            // apply to the corresponding Bezier Curve Components
            bezierCurves[i].ApplyMainBezierKnots(bezierKnots[i]);
        }
    }

    void DetermineRandomBezierSegments()
    {
        // define gap (# non-rand. segments) between two randomized segments 
        int gap = roadType == RoadType.Simple ?
        8 : roadType == RoadType.Medium ?
        4 : 2;

        // calculate how many points have primary randomization
        int amountOfPoints = (segmentCount / gap) - 1;  // -1 to prevent last segment to have scattering
        primaryScatterPoints = new int[amountOfPoints];
        System.Random random = new System.Random();
        int start = Math.Max((int)(random.NextDouble() * (gap - 1)), 3);

        for (int i = 0; i < amountOfPoints; i++)
        {
            primaryScatterPoints[i] = i * gap + start;
        }

    }

    /// <summary>
    /// Generates the random deviations for all road segments except the first
    /// TODO: adjust and rename according to new functionality 
    /// </summary>
    private void GenerateRandomNumbers()
    {
        randomNumbers = new float[segmentCount];
        // no randomization on the last segment's end point
        randomNumbers[segmentCount - 1] = 0;
        for (int i = 0; i < segmentCount - 1; i++)
        {
            // determine if the segment has randomization
            bool randomize = primaryScatterPoints.Contains(i);

            System.Random random = new System.Random();

            // apply randomization based on scattering range
            if (!randomize) randomNumbers[i] = 0;
            else randomNumbers[i] = (float)(random.NextDouble() * 2.0 * scatteringRange - scatteringRange);
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

        // calculate the displacement factor from random deviations
        float deviation1 = (100.0f + randomNumbers[segment]) / 100.0f;

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
        float amp = (4.0f / 3.0f) * Mathf.Tan((float)(Math.PI / (2.0f * segmentCount)));

        // C1 CONTINUITY WITH RANDOMNES        
        // use the previous quadrouple of Bezier knots for calculating the curent knots
        Vector3 p_1;
        if (segment == 0) p_1 = amp * Vector3.Cross(p_0, normal) + p_0;  //  keep the first segment straight for starting
        else p_1 = p_0 - (bezierKnots[segment - 1][2] - p_0);  //  to p_0 add the neg. vector betw. the prev. segment's p_2 and current p_0
        Vector3 p_2;
        if (segment == segmentCount - 1) p_2 = p_3 - (bezierKnots[0][1] - p_3);  //  same as for p_1 but mirrored
        else p_2 = (amp * Vector3.Cross(normal, p_3) + p_3) * deviation1;  //  1) calculate perfect circle knot 2) multiply with rand. dev.

        return new Vector3[] { p_0, p_1, p_2, p_3 };
    }

    Vector3[] CircularCubicBezierKnotsV2(int segment)
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

        // CALCULATE THE INTERMEDIARY KNOTS
        // define normal vector
        Vector3 normal = new(0, 1, 0);
        // calculate the amplitude factor for the tangents. 
        // It is directly related to the number of segments for the circle.
        // from Stackoverflow: (4/3)*tan(pi/(2n)) (https://stackoverflow.com/questions/1734745/how-to-create-circle-with-b%C3%A9zier-curves)
        float amp = (4.0f / 3.0f) * Mathf.Tan((float)(Math.PI / (2.0f * segmentCount)));

        // calculate the knots
        Vector3 p_1 = amp * Vector3.Cross(p_0, normal) + p_0;
        Vector3 p_2 = amp * Vector3.Cross(normal, p_3) + p_3;

        return new Vector3[] { p_0, p_1, p_2, p_3 };
    }

    /// <summary>
    /// Randomize Bezier end knots based on the probability for randomization and the road type
    /// </summary>
    void RandomizeBezierKnots()
    {
        System.Random random = new System.Random();

        for (int i = 0; i < bezierKnots.Length; i++)
        {
            // calculate the displacement factor from random deviations
            float deviation = (100.0f + randomNumbers[i]) / 100.0f;

            bezierKnots[i][3] = bezierKnots[i][3] * deviation;

            bezierKnots[i][0] = bezierKnots[(segmentCount + i - 1) % segmentCount][3];
            bezierCurves[i].ApplyMainBezierKnots(bezierKnots[i]);
        }
    }

    /// <summary>
    /// Adjusts all cubic Bezier segments that were not part of the primary random scattering, to achieve C1 continuity.
    /// This creates a more relaxed road, based on the road type.
    /// The adjusted segments are subject to secondary random scattering.
    /// </summary>
    void AdjustKnotsWithScattering()
    {

        for (int i = 0; i <= primaryScatterPoints.Length; i++)
        {
            // define how many segments are between each pair of randomized bezier knots (or the start point) need to be interpolated
            int targetSegments;
            if (i == 0) targetSegments = primaryScatterPoints[i];
            else if (i == primaryScatterPoints.Length) targetSegments = segmentCount - 2 - primaryScatterPoints[i - 1];
            else targetSegments = primaryScatterPoints[i] - primaryScatterPoints[i - 1] - 1;

            // interpolate the segments between the randomized knots
            if (i == 0)
            {
                // interpolate the segments form the start to the first randomized point
                InterPolateBezierSegments(0, primaryScatterPoints[i] + 1, targetSegments);
            }
            else if (i == primaryScatterPoints.Length)
            {
                // interpolate the segments form the last randomized point to the end
                InterPolateBezierSegments(primaryScatterPoints[i - 1] + 1, 0, targetSegments);
            }
            else
            {
                // interpolate the segments between the last randomized point and the current
                InterPolateBezierSegments(
                    primaryScatterPoints[i - 1] + 1,
                    primaryScatterPoints[i] + 1,
                    targetSegments);
            }

            // if (i == 0)
            // {
            //     for (int j = 0; j < primaryScatterPoints[i]; j++)
            //     {
            //         // calculate the displacement factor for bezier segment j for intersecting 
            //         // the line between segment 0's START knot and the FIRST randomized segment's END knot.
            //         AdjustedCubicBezierKnots(j, bezierKnots[0][0], bezierKnots[primaryScatterPoints[i]][3]);
            //     }
            // }
            // // else if (i == primaryScatterPoints.Length)
            // // {
            // //     for (int j = primaryScatterPoints[i - 1] + 1; j < segmentCount; j++)
            // //     {
            // //         // calculate the displacement factor for bezier segment j for intersecting 
            // //         // the line between the LAST randomized segment's END knot and segment 0's START knot.
            // //     }
            // // }
            // // else
            // // {
            // //     for (int j = primaryScatterPoints[i - 1] + 1; j < primaryScatterPoints[i]; j++)
            // //     {
            // //         // calculate the displacement factor for bezier segment j for intersecting 
            // //         // the line between the (i-1)-th randomized segment's END knot and the i-th randomized segment's END knot.
            // //     }
            // // }

        }
    }
    /// <summary>
    /// Create the new coordinates in the infinite set of points between start and end, defined as p' = start + t(end - start)
    /// Always uses the first control knot of the start and end Bezier curves, referenced by with an index value.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="amountSegments"></param>
    void InterPolateBezierSegments(int start, int end, int amountSegments)
    {
        // define the sampling factor
        float sampling = 1 / (amountSegments + 1);

        // calculate vector between start and end

    }
    void AdjustedCubicBezierKnots(int segment)
    {

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

