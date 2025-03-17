// Script to create a Road segment based on Bezier curves
// 
// First, creates the equidistant lines from the middle line that is defined as a Bezier curve.
// Then, it creates perfectly equidistant offset curve lines left and right of the middle line. 
//
// Then, right and left Bezier curves with given offset start and end points are created. 
// These can be manually fitted to match the equidistant lines as closely as possible.
// 
// ISSUES and Improvement ideas: 
// - make the bezier curves editable in Scene with gizmos
//
// Author: Ammar Hammad
// 

using System.Net;
using autoware_vehicle_msgs.msg;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode()]
public class BezierCurveExample : MonoBehaviour
{
    // Control Points for the middle line Bezier curve
    [Header("Control Points for the Middle Bezier Curve")]
    public Vector3 pm_0;  // Start point
    public Vector3 pm_1;  // Control point 1
    public Vector3 pm_2;  // Control point 2
    public Vector3 pm_3;  // End point

    // for recognizing state changes
    private Vector3 _last_pm_0, _last_pm_1, _last_pm_2, _last_pm_3;

    private BezierCurve bezierCurve;

    [Header("Resolution (number of segments for every cubic Bezier curve)")]
    public int resolution = 20; // Number of segments
    // for recognizing state changes
    private int _last_resolution;

    // Control Points for the right line Bezier curve (approximation of the equidistant lines)
    [Header("Control Points for the Right Bezier Curve")]
    public Vector3 pr_0;
    public Vector3 pr_1;
    public Vector3 pr_2;
    public Vector3 pr_3;

    private BezierCurve rightBezTwin;

    // Control Points for the right line Bezier curve (approximation of the equidistant lines)
    [Header("Control Points for the Left Bezier Curve")]
    public Vector3 pl_0;
    public Vector3 pl_1;
    public Vector3 pl_2;
    public Vector3 pl_3;

    private BezierCurve leftBezTwin;

    private Vector3[] curvePoints;
    private Vector3[] rightPoints;
    private Vector3[] leftPoints;
    private Vector3[] sampledFittedBezierR;
    private Vector3[] sampledFittedBezierL;


    void OnValidate()
    {
        // recalculate everything, if the middle bezier curve's parameter or the resolution changed
        if (pm_0 != _last_pm_0 || pm_1 != _last_pm_1 || pm_2 != _last_pm_2 || pm_3 != _last_pm_3 || resolution != _last_resolution)
        {
            CompleteSetup();
        }
        else
        {
            // ONLY Update the twin cubic Bezier curves and their sampled points
            UpdateTwinBezierSamplePoints();
        }
    }

    public void ApplyMainBezierKnots(Vector3[] knots)
    {
        // safety check befor assigning new knot values
        if (knots.Length == 4)
        {
            (pm_0, pm_1, pm_2, pm_3) = (knots[0], knots[1], knots[2], knots[3]);
        }
        else
        {
            Debug.LogError("Format mismatch. The Bezier knots cannot be applied");
        }
        // setup the Bezier Curve data
        CompleteSetup();
    }

    /// <summary>
    /// Initiate the complete setup with curve and line creation, creation of twin Bezier curves, 
    /// and the lines for the sampled twin bezier cirves, based on the current main Bezier curve.
    /// </summary>
    void CompleteSetup()
    {
        // CALCULATE ALL CURVES AND LINES based on the original Bezier curve
        CalculateCurves();
        // UPDATE THE BEZIER CURVES FOR APPROXIMATION OF THE LEFT AND RIGHT EQUIDISTANT LINES
        UpdateBezierTwins();
        // UPDATE the arrays for the sampled twin bezier curves
        UpdateTwinBezierSamplePoints();
    }

    /// <summary>
    /// Calculates all the Curves based on the middle cubic Bezier curve:
    /// 1) the 2 perfectly equidistant lines left and right to the middle Bezier curve
    /// 2) initiate update of the twin Bezier curves that are prefitted to match the equidistant lines. 
    /// </summary>
    public void CalculateCurves()
    {
        bezierCurve = new BezierCurve(pm_0, pm_1, pm_2, pm_3);
        // save the new state to the _last variables
        (_last_pm_0, _last_pm_1, _last_pm_2, _last_pm_3, _last_resolution) = (pm_0, pm_1, pm_2, pm_3, resolution);
        curvePoints = new Vector3[resolution];
        rightPoints = new Vector3[resolution];
        leftPoints = new Vector3[resolution];
        sampledFittedBezierR = new Vector3[resolution];
        sampledFittedBezierL = new Vector3[resolution];

        // sample the Bezier curve over t
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1); // Ensure last point is at t = 1
            curvePoints[i] = CurveUtility.EvaluatePosition(bezierCurve, t);
        }

        // define the normal vertical vector for the complete 2d curve
        Vector3 normal = new Vector3(0, 1, 0);

        // CALCULATE EUQIDISTANT LINES LEFT AND RIGHT OF THE BEZIER CURVE
        // for each sampled curve point, create the opposing cross products to obtain points left and right of the curve
        for (int i = 0; i < resolution; i++)
        {
            // calculate local curve direction vector
            Vector3 curveVector;
            if (i == resolution - 1) curveVector = curvePoints[i] - curvePoints[i - 1];
            else curveVector = curvePoints[i + 1] - curvePoints[i];

            // calculate cross product of normal and the normalized curve vector and add start posistion to it
            Vector3 xProductRight = Vector3.Cross(normal, curveVector.normalized) + curvePoints[i];  // Unity is left-hand
            Vector3 xProductLeft = Vector3.Cross(curveVector.normalized, normal) + curvePoints[i];

            rightPoints[i] = xProductRight;
            leftPoints[i] = xProductLeft;
        }

    }

    /// <summary>
    /// Updates the twin Bezier curves left and right to the middle Beziercurve.
    /// 1) Updates the control knots of the twin Bezier curves
    /// 2) Initiate the Update of Bezier Twin Curves
    /// </summary>
    void UpdateBezierTwins()
    {
        // create control knots for the twin curves left and right of the original curve
        (pr_0, pr_1, pr_2, pr_3) = BezierTwinKnots(bezierCurve, true);
        (pl_0, pl_1, pl_2, pl_3) = BezierTwinKnots(bezierCurve, false);
    }

    /// <summary>
    /// Create or Update the sampled points for the pre-fitted twin cubic Bezier curves.
    /// </summary>
    void UpdateTwinBezierSamplePoints()
    {
        rightBezTwin = new BezierCurve(pr_0, pr_1, pr_2, pr_3);
        leftBezTwin = new BezierCurve(pl_0, pl_1, pl_2, pl_3);

        // sample the Bezier curve over t
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1); // Ensure last point is at t = 1
            sampledFittedBezierR[i] = CurveUtility.EvaluatePosition(rightBezTwin, t);
            sampledFittedBezierL[i] = CurveUtility.EvaluatePosition(leftBezTwin, t);
        }
    }

    /// <summary>
    /// Claculates the cubic twin Bezier curve's control knots using the middle Bezier curve's start and end tangent vector. 
    /// The start and end control points are not yet fitted to the curve.
    /// </summary>
    /// <param name="origBezCurve">the middle cubic Bezier curve</param>
    /// <param name="right">if true, the right curve's control knots should be calculated, else the left's</param>
    /// <returns>the 4 control points for a cubic Bezier curve</returns>
    (Vector3, Vector3, Vector3, Vector3) BezierTwinKnots(BezierCurve origBezCurve, bool right)
    {
        // define normal vector
        Vector3 normal = new Vector3(0, 1, 0);

        // TODO: define visibility and default values of these arbitrary factors (maybe easier to manipulate the curve than with knots)
        // these factors define the amplitude of the tangents
        float factorS = 0.45f;
        float factorE = 0.285f;

        // transform start and endpoint
        // get original start and end tangents
        Vector3 startTangent = factorS * CurveUtility.EvaluateTangent(origBezCurve, 0.00f);
        Vector3 endTangent = -1.0f * factorE * CurveUtility.EvaluateTangent(origBezCurve, 1.00f);

        // define orientation (-1 or 1) depending on bool "right" being true or false
        float orientation = right == true ? 1.0f : -1.0f;

        // calculate twin's start and end point the cross product between normal and tangent vector 
        // and add original start and end point to it
        // USING bxa = -axb = (-1*a)xb, to switch between axb and bxa simple by factor
        Vector3 startPoint = Vector3.Cross(orientation * normal, startTangent.normalized) + pm_0;
        Vector3 endPoint = Vector3.Cross(orientation * endTangent.normalized, normal) + pm_3;

        // calculate the intermediary points p_1 and p_2
        Vector3 p_1 = startPoint + startTangent;
        Vector3 p_2 = endPoint + endTangent;

        // BezierCurve twinBezier = new BezierCurve(startPoint, p_1, p_2, endPoint);

        // return bezier knots;
        return (startPoint, p_1, p_2, endPoint);
    }

    void OnDrawGizmos()
    {
        if (curvePoints == null || curvePoints.Length < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < curvePoints.Length - 1; i++)
        {
            Gizmos.DrawLine(curvePoints[i], curvePoints[i + 1]);
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < rightPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(rightPoints[i], rightPoints[i + 1]);
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < leftPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(leftPoints[i], leftPoints[i + 1]);
        }

        // draw the sampled right twin Bezier curve 
        Gizmos.color = Color.red;
        for (int i = 0; i < sampledFittedBezierR.Length - 1; i++)
        {
            Gizmos.DrawLine(sampledFittedBezierR[i], sampledFittedBezierR[i + 1]);
        }

        // draw the sampled left twin Bezier curve
        Gizmos.color = Color.blue;
        for (int i = 0; i < sampledFittedBezierL.Length - 1; i++)
        {
            Gizmos.DrawLine(sampledFittedBezierL[i], sampledFittedBezierL[i + 1]);
        }
    }

    public Vector3[] GetLeftPoints() => leftPoints;
    public Vector3[] GetRightPoints() => rightPoints;
}

