using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BezierRoadManager))]
public class BezierRoadEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default Inspector
        DrawDefaultInspector();

        // Reference the target script
        BezierRoadManager script = (BezierRoadManager)target;

        // Section 1: Curve Calculation
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Update Road", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this button to adjust the road mesh to the new control points.", MessageType.Info);

        if (GUILayout.Button("Update Road", GUILayout.Height(25)))
        {
            script.UpdateRoadMesh();
        }

    }
}