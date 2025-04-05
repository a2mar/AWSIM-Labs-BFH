/// State Helper class for Bezier Road
///

using UnityEngine;

public class BezierRoadState
{
    // Bezier Vectors [row][col], each row defines the BezierKnots for a cubic bezier curve.
    public Vector3[][] bezierKnots;
    public float[] randomNumbers;               // random number based on scattering range
    public int[] primaryScatterPoints;          // indices of the segments with primary randomization
    public int segmentCount;                    // amount of segments for the whole road
    public float radius;                        // radius of the road as a perfect circle, before randomization
    public int triggersPerSegment;              // segment triggers for every trigger

    // Bezier Curve Data and Mesh
    public BezierRoadMesh[] roadMeshes;         // road mesh component
    public BezierCurveGroup[] bezierCurves;     // Bezier Curve Data component

    // game objects
    public GameObject[] bezierCurveObjects;     // GObj for curves and lanes data
    public GameObject[] roadMeshObjects;        // GObj for road mesh
    public GameObject[] roadSegments;           // GObj for road segment

    // transform of the game object that holds the Bezier Road
    public Transform transform;

    // the manager component
    public Component manager;

    public BezierRoadState(Transform transform, Component manager, int segmentCount, float radius, int triggersPerSegment)
    {
        this.transform = transform;
        this.manager = manager;
        this.segmentCount = segmentCount;
        this.radius = radius;
        this.triggersPerSegment = triggersPerSegment;
    }
}
