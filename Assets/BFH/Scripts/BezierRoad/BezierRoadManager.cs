// Manages the creation of a Road System made up by road segments created with Bezier curves.
// The road system is composed of multiple tripplets of approximately equidistant cubic Bezier Curves, and their
// respective generated road mesh.
//
// TODO:
//  - wrap debug statements into compiler macros
//  
// ISSUES:
//  - different lengths of curve segments after interpolation
// 
// Author: Ammar Hammad

using UnityEngine;
using System.Net.Http;
using System.Linq;




#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode()]
public class BezierRoadManager : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private BezierRoadState roadState;

    // variables added to be added to state
    private int segmentCount = 64;  // at least 16 for cricle creation
    private float radius = 240.0f;
    private int triggersPerSegment = 4;

    // how much the control knots can be randomply scattered
    [Header("Range of Random Scattering of Bezier Control Knots (0 means no random scattering)")]
    public float scatteringRange = 0f;
    private float maxScattering = 40f;

    // secondary scattering
    [Header("Range of Secondary Random Scattering of Bezier Control Knots (0 means no secondary random scattering)")]
    public float secundaryScatteringRange = 10f;

    public enum RoadType
    {
        Simple,  // randomize every 8th Bezier end control knot
        Medium,  // randomize every 4th Bezier end control knot

    }

    [Header("Road Type")]
    public RoadType roadType = RoadType.Medium;

    // component state variables
    [SerializeField, HideInInspector]
    private float _last_scatRange, _last_secScatRange;
    [SerializeField, HideInInspector]
    private RoadType _last_roadType;
    [SerializeField, HideInInspector]
    private bool initialized;

    // // for RL
    private TriggerCreator[] triggerCreators;

    void Awake()
    {
        if (roadState == null)
        {
            roadState = new BezierRoadState(this.transform, this, segmentCount, radius, triggersPerSegment);
        }

        if (Application.isPlaying || initialized)
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



        // save component state
        SaveState();
        // create BezierCurveGroup and BezierRoadMesh dynamically
        BezierComponentBuilder.AssignComponents(roadState);
        // create Vectors for the Bezier curves and update the Bezier Curve Components
        BezierRoadGeometry.InitializeBezierKnots(roadState);
        // generate the initial road mesh
        UpdateRoadMesh();
        // generate the trigger colliders
        GenerateTriggerColliders();
        // set initialized to true
        initialized = true;

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
        // validate the scattering range, bound it by maxScattering
        if (scatteringRange > maxScattering) scatteringRange = maxScattering;
        // create BezierCurveGroup and BezierRoadMesh dynamically
        BezierComponentBuilder.AssignComponents(roadState);
        // deals with special case that scatteringRange == 0
        if (!StateChanged() && scatteringRange != 0) return;

        // PASSED FOR UPDATES

        // create Vectors for the Bezier curves and update the Bezier Curve Components
        BezierRoadGeometry.InitializeBezierKnots(roadState);

        // skip this if scatteringRange == 0, all random deviation will be 1
        if (scatteringRange != 0)
        {
            // ALGORITHM FOR RANDOM ROAD CREATION
            GenerateRandomRoadPolygon();
            // Relax the bent curves
            int iterations = (int)Mathf.Ceil(scatteringRange / 20f);
            for (int i = 0; i <= iterations; i++)
            {
                BezierRoadGeometry.RelaxCurves(roadState);
            }
            // update all Bezier segment components
            for (int i = 0; i < roadState.segmentCount; i++) roadState.bezierCurves[i].ApplyMainBezierKnots(roadState.bezierKnots[i]);

            // show the primary scatter points
            // for (int i = 0; i < roadState.primaryScatterPoints.Length; i++) Debug.Log($"scatterPoints[{i}] = {roadState.primaryScatterPoints[i]}");
        }

        // save state
        SaveState();
        // generate the initial road mesh
        UpdateRoadMesh();
        // generate the trigger colliders
        GenerateTriggerColliders();
    }

    private void GenerateRandomRoadPolygon()
    {
        // 1. Define the number n of corners, in cluding start point of road (at x > 0, y == 0, z == 0)
        int cornerCount = RandomTools.CornerCount(roadState, roadType);
        // Debug.Log($"cornerCount: {cornerCount}");
        // 2. Define length of segments, lenght of their sum and the length of edges
        float stretchFactor = BezUtils.StretchFactor(roadType == RoadType.Simple, scatteringRange);
        // Debug.Log($"strech factor is: {stretchFactor}");
        float segLength = BezierRoadGeometry.SegmentLength(roadState, stretchFactor);
        // 3. Define length of every individual edge
        int[] segmentPerEdge = RandomTools.SegmentsPerEdge(roadState, roadType, cornerCount);
        // 4. Define random deviations and 5. Calculate positions of the corners
        bool roadDone = false;
        bool vectorsCorrupt = false;
        bool cornersTooClose = false;
        bool tangentsTooClose = false;
        do
        {
            GenerateRandomRoadPolygon(segmentPerEdge, cornerCount, segLength);
            // 6. Adjust the knots between the corners
            BezierRoadGeometry.AdjustKnots(roadState);

            vectorsCorrupt = BezWorkarounds.RoadVectorsCorrupt(roadState);
            if (vectorsCorrupt)
            {
                Debug.Log($"Corrupt vectors detected. Rebuilding...");
                continue;
            }
            cornersTooClose = BezWorkarounds.CornersTooClose(roadState, segLength);
            if (cornersTooClose)
            {
                Debug.Log($"Very Close Corners detected. Rebuilding...");
                continue;
            }
            tangentsTooClose = BezWorkarounds.TangentsTooCLose(roadState);
            if (tangentsTooClose)
            {
                Debug.Log($"Very tight tangents detected. Rebuilding...");
                continue;
            }

            // 7. Check the road and repeat if necessary
            roadDone = BezWorkarounds.RoadUnbroken(roadState, stretchFactor);
            // Debug.Log($"road done: {roadDone}");
            if (!roadDone) Debug.Log("Road has errors. Rebuilding...");
        } while (!roadDone);
    }

    private void GenerateRandomRoadPolygon(int[] segmentPerEdge, int cornerCount, float segLength)
    {
        // generate random numbers based on scattering
        RandomTools.GenerateRandomNumbers(roadState, scatteringRange, secundaryScatteringRange);
        float[] deviations = RandomTools.Deviations(roadState);
        // Calculate positions of the corners (by solving for the angles iteratively, given the edge-length and deviation)
        BezierRoadGeometry.CalculateRoadCornerPositions(roadState, deviations, segmentPerEdge, cornerCount, segLength);
    }

    public void UpdateRoadMesh()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            // Debug.Log($"!!!!!!!!!!!!!!!!!!!!!The road mesh is: {leftPoints[leftPoints.Length - 1].x}");
            roadState.roadMeshes[i].GenerateRoadMesh(roadState.bezierCurves[i].GetLeftPoints(), roadState.bezierCurves[i].GetRightPoints());
        }
    }

    public void CallBezierTwinApproximation()
    {
        if (roadState.primaryScatterPoints == null)
        {
            Debug.Log("No randomization has been applied. Skipping Approximation...");
            return;
        }

        // roadState.bezierCurves[segmentCount - 1].ApproximateSecondaryCurves();
        // float start = Time.realtimeSinceStartup;
        foreach (BezierCurveGroup curve in roadState.bezierCurves)
        {
            curve.ApproximateSecondaryCurves();
        }
        // float duration = Time.realtimeSinceStartup - start;
        // Debug.LogError($"Approximation took {duration * 1000f} ms");
    }

    void GenerateTriggerColliders()
    {
        if (triggerCreators == null)  // CREATE NEW
        {
            // initialize Array
            triggerCreators = new TriggerCreator[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                // initialize Trigger Creators
                triggerCreators[i] = new TriggerCreator(i);
                // create Trigger Colliders
                triggerCreators[i].CreateTriggerColliders(roadState);
            }
        }
        else  // UPDATE
        {
            for (int i = 0; i < segmentCount; i++)
            {
                // update trigger colliders
                triggerCreators[i].CreateTriggerColliders(roadState);
            }
        }

    }

    /// <summary>
    /// Get all the Bezier curve groupof the road.
    /// </summary>
    /// <returns>Array of BezierCurveGroups objects, or emtpy array if not initialized</returns>
    public BezierCurveGroup[] GetBezierCurves()
    {
        // return empty array if not initialized yet
        //return bezierCurves != null ? bezierCurves : new BezierCurveGroup[] { };
        // If the array is uninitialized, find all existing ones
        if (roadState.bezierCurves == null || roadState.bezierCurves.Length == 0)
        {
            roadState.bezierCurves = GetComponentsInChildren<BezierCurveGroup>();
        }

        return roadState.bezierCurves;
    }

}