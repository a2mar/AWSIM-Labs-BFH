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
    private BezierCurveExample bezierCurve;
    private BezierRoadMesh roadMesh;
    private GameObject bezierCurveObject;  // GO for curves and lanes data
    private GameObject roadMeshObject;  // GO for road mesh
    private GameObject roadSegment;  // GO for road segment


    void Start()
    {
        // create BezierCurveExample and BezierRoadMesh dynamically
        AssignComponents();

        // generate the initial road mesh
        UpdateRoadMesh();
    }

    void AssignComponents()
    {
        // check if the objects already exist, if not, create them
        if (roadSegment == null)
        {
            roadSegment = new GameObject("RoadSegment");
            roadSegment.transform.parent = this.transform;
        }

        if (bezierCurveObject == null)
        {
            bezierCurveObject = new GameObject("BezierCurveContainer");
            bezierCurveObject.transform.parent = roadSegment.transform; // set as a child of the manager
            bezierCurve = bezierCurveObject.AddComponent<BezierCurveExample>();
        }
        else
        {
            bezierCurve = bezierCurveObject.GetComponent<BezierCurveExample>();
        }

        if (roadMeshObject == null)
        {
            roadMeshObject = new GameObject("RoadMeshContainer");
            roadMeshObject.transform.parent = roadSegment.transform; // set as a child of the manager
            roadMesh = roadMeshObject.AddComponent<BezierRoadMesh>();
        }
        else
        {
            roadMesh = roadMeshObject.GetComponent<BezierRoadMesh>();
        }
    }

    void Update()
    {
        // Update road mesh dynamically (if curves change)
        UpdateRoadMesh();
    }

    private void UpdateRoadMesh()
    {
        roadMesh.GenerateRoadMesh(bezierCurve.GetLeftPoints(), bezierCurve.GetRightPoints());
    }
}

