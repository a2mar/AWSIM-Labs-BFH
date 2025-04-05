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
// 
// Author: Ammar Hammad

using UnityEngine;


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
    private float radius = 160.0f;
    private int triggersPerSegment = 4;

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

    // component state variables
    [SerializeField, HideInInspector]
    private float _last_scatRange, _last_secScatRange;
    [SerializeField, HideInInspector]
    private RoadType _last_roadType;
    [SerializeField, HideInInspector]
    private bool initialized;

    // // for RL
    // private TriggerCreator[] triggerCreators;

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
            // determin which Bezier segments to randomize with primary scattering, according to road type
            RandomTools.DetermineRandomBezierSegments(roadState, roadType);
            // generate deviation factors for random scattering
            RandomTools.GenerateRandomNumbers(roadState, scatteringRange, secundaryScatteringRange);
            // primary random scattering of a fraction of Bezier segments
            BezierRoadGeometry.ApplyRandomToBezierKnots(roadState);
            // adjust all other knots to the randomized knots, relaxing the curve, but add secondary random scattering
            BezierRoadGeometry.AdjustKnotsWithScattering(roadState);
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
            roadState.roadMeshes[i].GenerateRoadMesh(roadState.bezierCurves[i].GetLeftPoints(), roadState.bezierCurves[i].GetRightPoints());
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