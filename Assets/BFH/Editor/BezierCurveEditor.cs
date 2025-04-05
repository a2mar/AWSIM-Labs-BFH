using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BezierCurveGroup))]
public class BezierCurveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default Inspector
        DrawDefaultInspector();

        // Reference the target script
        BezierCurveGroup script = (BezierCurveGroup)target;

        // Section 1: Curve Calculation
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Curve Operations", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use these buttons to update the curve calculations.", MessageType.Info);

        if (GUILayout.Button("Calculate Curves", GUILayout.Height(25)))
        {
            script.CalculateCurves();
        }

    }
}

