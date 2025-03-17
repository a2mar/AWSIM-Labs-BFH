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

    void Start()
    {
        // create BezierCurveExample and BezierRoadMesh dynamically
        bezierCurve = gameObject.AddComponent<BezierCurveExample>();
        roadMesh = gameObject.AddComponent<BezierRoadMesh>();

        // ensure components exist before proceeding
        if (bezierCurve == null || roadMesh == null)
        {
            Debug.LogError("BezierCurveExample or BezierRoadMesh could not be created!");
            return;
        }

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
        roadMesh.GenerateRoadMesh(bezierCurve.GetLeftPoints(), bezierCurve.GetRightPoints());
    }
}

