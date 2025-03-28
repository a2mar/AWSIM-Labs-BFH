// Manages the creation of a Road System made up by road segments created with Bezier curves.
// The road system is composed of multiple tripplets of approximately equidistant cubic Bezier Curves, and their
// respective generated road mesh.
//
// TODO:
//  - implement secondary scattering algorithm
//  - adjust primary scattering
//  
// ISSUES:
//  - different lengths of curve segments after interpolation
//  - high coupling through the use of many global variables
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
    private int segmentCount = 64;  // at least 16 for cricle creation
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

    // secondary scattering
    [Header("Range of Secondary Random Scattering of Bezier Control Knots (0 means no secondary random scattering)")]
    public float secundaryScatteringRange = 10f;

    public enum RoadType
    {
        Simple,  // randomize every 8th Bezier end control knot
        Medium,  // randomize every 4th Bezier end control knot

    }

    [Header("Road Type")]
    public RoadType roadType = RoadType.Simple;

    // state variables
    private float _last_scatRange, _last_secScatRange;
    private RoadType _last_roadType;

    private float[] randomNumbers;
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
        // check segmentCount
        if (segmentCount < 32)
        {
            Debug.LogError($"The segment count of {segmentCount} is to low for a reliable execution of the RoadManager component."
            + " Please, increase the number to a minumum of 32.");
            return;
        }
        // save state
        SaveState();
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

    void SaveState() => (_last_scatRange, _last_secScatRange, _last_roadType) = (scatteringRange, secundaryScatteringRange, roadType);
    bool StateChanged()
    {
        return _last_scatRange != scatteringRange || _last_secScatRange != secundaryScatteringRange || _last_roadType != roadType;
    }

    /// <summary>
    /// Updates the whole road system according to the changed state (scattering range, secondary scattering range, road type)
    /// </summary>
    void UpdateRoad()
    {
        // deals with special case that scatteringRange == 0
        if (!StateChanged() && scatteringRange != 0) return;

        // create Vectors for the Bezier curves and update the Bezier Curve Components
        InitializeBezierKnots();

        // skip this if scatteringRange == 0, all random deviation will be 1
        if (scatteringRange != 0)
        {
            // determin which Bezier segments to randomize with primary scattering, according to road type
            DetermineRandomBezierSegments();
            // generate deviation factors for random scattering
            GenerateRandomNumbers();
            // primary random scattering of a fraction of Bezier segments
            RandomizeBezierKnots();
            // adjust all other knots to the randomized knots, relaxing the curve, but add secondary random scattering
            AdjustKnotsWithScatteringV2();
        }

        // save state
        SaveState();
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
            // bezierKnots[i] = CubicBezierKnots(previous);
            bezierKnots[i] = CircularCubicBezierKnotsV2(i);
            // apply to the corresponding Bezier Curve Components
            bezierCurves[i].ApplyMainBezierKnots(bezierKnots[i]);
        }
    }

    /// <summary>
    /// Randomly chooses the indices of the bezier curve segments to be randomized with primary randomization.
    /// The amount of randomized curve segments depends on the road type, as well as on the segment count
    /// </summary>
    void DetermineRandomBezierSegments()
    {
        // define gap (# non-rand. segments) between two randomized segments 
        int gap = roadType == RoadType.Simple ? 8 : 4;

        // calculate how many points have primary randomization
        int amountOfPoints = (segmentCount / gap) - 1;  // -1 to prevent last segment to have scattering
        // if amount of points is 1, double the gap forcibly
        if (amountOfPoints < 2)
        {
            // decrease the gap and amount of points
            gap /= 2;
            amountOfPoints = (segmentCount / gap) - 1;
        }
        primaryScatterPoints = new int[amountOfPoints];
        // primaryScatterPoints = new int[] { 3, 11, 19 };

        System.Random random = new System.Random();
        int start = Math.Max((int)(random.NextDouble() * (gap - 1)), 3);

        for (int i = 0; i < amountOfPoints; i++)
        {
            // check 
            primaryScatterPoints[i] = i * gap + start;
            // primaryScatterPoints[i] = i * gap + 3;
            // Debug.Log($"the scatter point determined is: {primaryScatterPoints[i]}");
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
            // determine if the segment has PRIMARY randomization
            bool primaryRandomization = primaryScatterPoints.Contains(i);
            
            // determine if segment has SECONDARY randomization (no randomization on segments neighbouring primarily randomized segments) 
            // modulus for array boundary safety
            bool secondaryRandomization =
                !primaryScatterPoints.Contains((segmentCount + (i - 1)) % segmentCount) ||
                !primaryScatterPoints.Contains(i) ||
                !primaryScatterPoints.Contains((segmentCount + i + 1) % segmentCount);

            System.Random random = new System.Random();

            // crete randomization based on scattering range
            if (primaryRandomization)
            {
                // generate random number for primary scattering
                randomNumbers[i] = (float)(random.NextDouble() * 2.0 * scatteringRange - scatteringRange);
            }
            else if (secondaryRandomization)
            {
                // generate random number in range of secondary scattering
                randomNumbers[i] = (float)(random.NextDouble() * 2.0 * secundaryScatteringRange - secundaryScatteringRange);
            }
            else
            {
                // no randomization
                randomNumbers[i] = 0;
            }
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

    /// <summary>
    /// Calculates the 4 control knots of a Bezier curve for a predifined angle and radius for a circular race track.
    /// </summary>
    /// <param name="segment">the index of the road segment for which the Bezier curve knots are calculated</param>
    /// <returns></returns>
    Vector3[] CircularCubicBezierKnotsV2(int segment)
    {
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

            // add random deviation to end knot of segment
            bezierKnots[i][3] = bezierKnots[i][3] * deviation;

            // also add deviation to the neighbouring knots
            bezierKnots[i][2] = bezierKnots[i][2] * deviation;
            bezierKnots[(segmentCount + i + 1) % segmentCount][1] = bezierKnots[(segmentCount + i + 1) % segmentCount][1] * deviation;

            // the start knot must mirror the end knot of the previous segment
            bezierKnots[i][0] = bezierKnots[(segmentCount + i - 1) % segmentCount][3];
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
            // Debug.LogError($"i is {i}");
            // define how many segments are between each pair of randomized bezier knots (or the start point) need to be interpolated
            int targetSegments;
            if (i == 0) targetSegments = primaryScatterPoints[i];
            else if (i == primaryScatterPoints.Length) targetSegments = segmentCount - 2 - primaryScatterPoints[i - 1];
            else targetSegments = primaryScatterPoints[i] - primaryScatterPoints[i - 1] - 1;


            // Debug.Log($"The i = {i} -th iteration determined this amount of segments: {targetSegments}");
            // Debug.Log($"The type of targetSegment is: {targetSegments.GetType()}");

            // interpolate the segments between the randomized knots
            if (i == 0)
            {
                // interpolate the segments form the start to the first randomized point
                InterPolateBezierSegments(0, primaryScatterPoints[i] + 2, targetSegments + 1);
            }
            else if (i == primaryScatterPoints.Length)
            {
                // DEBUG skip this
                // interpolate the segments form the last randomized point to the end
                InterPolateBezierSegments(primaryScatterPoints[i - 1] + 1, 0, targetSegments);
            }
            else if (i != primaryScatterPoints.Length)
            {
                // interpolate the segments between the last randomized point and the current
                InterPolateBezierSegments(
                    primaryScatterPoints[i - 1] + 1,
                    primaryScatterPoints[i] + 2,
                    targetSegments + 1);
            }


        }
    }

    /// <summary>
    /// Create the new coordinates in the infinite set of points between start and end, defined as p' = start + t(end - start)
    /// Always uses the first control knot of the start and end Bezier curves, referenced by with an index value.
    /// </summary>
    /// <param name="start">index of the Bezier segment where the adjustments starts</param>
    /// <param name="end">index of the Bezier Segment where the adjustment ends</param>
    /// <param name="amountSegments">the number of segments to be adjusted</param>
    void InterPolateBezierSegments(int start, int end, int amountSegments)
    {
        // Debug.LogError($"called interpolate with start: {start}, end: {end}, amount seg.: {amountSegments}");
        // define the sampling factor
        float sampling = (float)(1f / (float)(amountSegments + 1));

        // calculate vector between start and end
        Vector3 distance = bezierKnots[end][0] - bezierKnots[start][0];
        // Debug.LogError($"the distance Vector is {distance}");

        // iterate over the segments 
        for (int i = 1; i <= amountSegments; i++)
        {
            // calculate the new positions of the identical start and end knots between two segments, on the distance vector
            bezierKnots[start + i - 1][3] = bezierKnots[start][0] + i * sampling * distance;
            bezierKnots[start + i][0] = bezierKnots[start][0] + i * sampling * distance;

            // Debug.LogError($"the given start point is: {bezierKnots[start][0] }");
            // Debug.LogError($"the factor for the distance vector is {i * sampling}");

            // Debug.LogError($"the calculated end  point {bezierKnots[start + i - 1][3]}");
            // Debug.LogError($"the calculated start point {bezierKnots[start + i][0]}");

            // calculate the intermediary Bezier knots with arbitrary strength also on the distance vector
            // adjustment to p_2 of previous segment. LIKE: p_1 = p_0 - (previousSegment.p2 - p_0)
            // bezierKnots[start + i - 1][1] = bezierKnots[start][0] + (i - 0.75f) * sampling * distance;
            if (i == 1) bezierKnots[start][1] = 2 * bezierKnots[start][0] - bezierKnots[(segmentCount + start - 1) % segmentCount][2];
            else bezierKnots[start + i - 1][1] = 2 * bezierKnots[start + i - 1][0] - bezierKnots[start + i - 2][2];

            if (i == amountSegments)
            {
                bezierKnots[start + i - 1][2] = 2 * bezierKnots[start + i - 1][3]
                - bezierKnots[(segmentCount + start + i) % segmentCount][1];
            }
            else bezierKnots[start + i - 1][2] = bezierKnots[start][0] + (i - 0.25f) * sampling * distance;

            /////////////// FIX: C1 continuity in last segment must also be adjusted.

            // apply changes
            bezierCurves[start + i - 1].ApplyMainBezierKnots(bezierKnots[start + i - 1]);
            bezierCurves[start + i].ApplyMainBezierKnots(bezierKnots[start + i]);
        }


    }

    /// <summary>
    /// Alternative, more direct variant of Bezier segment adjustment
    /// </summary>
    void AdjustKnotsWithScatteringV2()
    {
        for (int i = 0; i <= primaryScatterPoints.Length; i++)
        {
            // CALCULATE amount of knots per interval
            // basic formula: intermediary segments have 4 knots, end segments have 3 knots to be adjusted
            // special case first intervall (between segment 0's start knot and first randomly scattered segments's end knot) 
            if (i == 0)
            {
                // segments = (primaryScatterPoints[i] - 1);//* 3 + 6;//4 + 8;//2 * 3;
                // InterpolateV2(0, primaryScatterPoints[i] + 1, knotCount);
                InterpolateV2(0, primaryScatterPoints[i] + 1);
            }
            // special case last interval (between last randomly scattered segments's end knot and the last segment's end knot)
            else if (i == primaryScatterPoints.Length)
            {
                // instead of the end knot of the last randomly scattered segment, use the geometrically identical knot 
                // of the next segment (next segment's start knot)
                // segments = (segmentCount - (primaryScatterPoints[i - 1] + 1) - 2); //* 3 + 6;//4 + 8;// 2 * 3;
                InterpolateV2((segmentCount + primaryScatterPoints[i - 1] + 1) % segmentCount, segmentCount);
            }
            // all normal cases (between two subsequent randomized end knots)
            else
            {
                // instead of the end knot of the previous randomly scattered segment, use the geometrically identical knot 
                // of its next segment (next segment's start knot)
                // segments = ((primaryScatterPoints[i] + 1) - (primaryScatterPoints[i - 1] + 1) - 2); //* 3 + 6;//4 + 8;//2 * 3;
                InterpolateV2(primaryScatterPoints[i - 1] + 1, primaryScatterPoints[i] + 1);
            }

            // debug the knotCount first:
            // Debug.Log($"the knotCount is: {segments}");
        }
        // update all Bezier segment components
        for (int i = 0; i < segmentCount; i++) bezierCurves[i].ApplyMainBezierKnots(bezierKnots[i]);
    }

    /// <summary>
    /// Interpolates the position of the Bezier control knots of the given curves.
    /// <br/> Scheme:
    /// \[start: {2}{3}]\[start + 1: (0)(1)(2)(3)]\ ... \[end - 2: (0)(1)(2)(3)]\[end - 1: (0){1}]\ 
    /// </summary>
    /// <param name="start">index of start curve</param>
    /// <param name="end">index of end curve still included (penultimate)</param>
    void InterpolateV2(int start, int end)
    {
        // calculate the number of full curve to be adjusted (subtract the incomplete curves at the end, see scheme)
        int segments = end - start - 2;
        // Debug.Log($"the knotCount is: {segments}");

        // define the sampling factor
        float sampling = 1f / (segments * 3 + 2 + 1);  // add 2 for {extra knots} at ends, add 1 for

        // calculate vector between start and end
        // DEBUGGING
        Vector3 distance = bezierKnots[end - 1][2] - bezierKnots[start][1];

        // calculate the knots 2 and 3 from the start curve
        bezierKnots[start][2] = bezierKnots[start][1] + 1 * sampling * distance;
        bezierKnots[start][3] = bezierKnots[start][1] + 2 * sampling * distance;

        // iterate over the bezier knots
        for (int i = 0; i < segments; i++)
        {
            // calculate the new position of the knots
            bezierKnots[(start + 1) + i][0] = bezierKnots[start][1] + (2 + 3 * i) * sampling * distance;
            bezierKnots[(start + 1) + i][1] = bezierKnots[start][1] + (3 + 3 * i) * sampling * distance;
            bezierKnots[(start + 1) + i][2] = bezierKnots[start][1] + (4 + 3 * i) * sampling * distance;
            bezierKnots[(start + 1) + i][3] = bezierKnots[start][1] + (5 + 3 * i) * sampling * distance;
        }

        // calculate the knots 0 and 1 from the curve [end - 1]
        bezierKnots[end - 1][0] = bezierKnots[start][1] + (2 + 3 * segments) * sampling * distance;
        bezierKnots[end - 1][1] = bezierKnots[start][1] + (3 + 3 * segments) * sampling * distance;
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

