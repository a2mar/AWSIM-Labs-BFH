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
// - make lane / road width dynamic
//
// Author: Ammar Hammad
// 

using Google.Protobuf.Reflection;
using rcl_interfaces.msg;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Splines;
using VehiclePhysics.UI;

[ExecuteInEditMode()]
public class BezierCurveGroup : MonoBehaviour
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
    public int resolution = 27; // Number of segments
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

    // scaling factor to adjust the width of the lane
    private float roadScaling = 4f;

    // epsilon for approximating the curves
    private const float EPSILON = 0.05f;

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

    /// <summary>
    /// Update the control knots of the main Bezier Curve.
    /// 
    /// Triggers a complete setup.
    /// </summary>
    /// <param name="knots"></param>
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
            float t = (float)i / (resolution - 1); // ensures last point is at t = 1
            curvePoints[i] = CurveUtility.EvaluatePosition(bezierCurve, t);
        }

        // define the normal vertical vector for the complete 2d curve
        Vector3 normal = new Vector3(0, 1, 0);

        // define scaling factor to adjust lane width


        // CALCULATE EUQIDISTANT LINES LEFT AND RIGHT OF THE BEZIER CURVE
        // for each sampled curve point, create the opposing cross products to obtain points left and right of the curve
        for (int i = 0; i < resolution; i++)
        {
            // calculate local curve direction vector
            Vector3 curveVector;
            // startpoints
            if (i == 0) curveVector = CurveUtility.EvaluateTangent(bezierCurve, 0.00f);
            // endpoints
            else if (i == resolution - 1) curveVector = CurveUtility.EvaluateTangent(bezierCurve, 1.00f);
            // intermediary points
            else curveVector = curvePoints[i + 1] - curvePoints[i];

            // calculate cross product of normal and the normalized curve vector and add start posistion to it
            Vector3 xProductRight = roadScaling * Vector3.Cross(normal, curveVector.normalized) + curvePoints[i];  // Unity is left-hand
            Vector3 xProductLeft = roadScaling * Vector3.Cross(curveVector.normalized, normal) + curvePoints[i];

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
        (pr_0, pr_1, pr_2, pr_3) = BezierTwinKnots(true);
        (pl_0, pl_1, pl_2, pl_3) = BezierTwinKnots(false);
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
    /// <param name="right">if true, the right curve's control knots should be calculated, else the left's</param>
    /// <returns>the 4 control points for a cubic Bezier curve</returns>
    (Vector3, Vector3, Vector3, Vector3) BezierTwinKnots(bool right)
    {
        // define normal vector
        Vector3 normal = new Vector3(0, 1, 0);

        // these factors define the amplitude of the tangents: in a cubix bezier control polygon, 
        // a tangent referes to one of 3 edges, therefor, 1/3 seems a good starting point for a minimally curved Bezier curve
        float factorS = 1f / 3;
        float factorE = 1f / 3;

        // transform start and endpoint
        // get original start and end tangents
        Vector3 startTangent = factorS * CurveUtility.EvaluateTangent(bezierCurve, 0.00f);
        Vector3 endTangent = -1.0f * factorE * CurveUtility.EvaluateTangent(bezierCurve, 1.00f);

        // define orientation (-1 or 1) depending on bool "right" being true or false
        float orientation = right == true ? 1.0f : -1.0f;

        // calculate twin's start and end point the cross product between normal and tangent vector 
        // and add original start and end point to it
        // USING bxa = -axb = (-1*a)xb, to switch between axb and bxa simple by factor
        Vector3 startPoint = roadScaling * Vector3.Cross(orientation * normal, startTangent.normalized) + pm_0;
        Vector3 endPoint = roadScaling * Vector3.Cross(orientation * endTangent.normalized, normal) + pm_3;

        // calculate the intermediary points p_1 and p_2
        Vector3 p_1 = startPoint + startTangent;
        Vector3 p_2 = endPoint + endTangent;

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
            Gizmos.DrawSphere(rightPoints[i], 0.1f);
        }
        Gizmos.DrawSphere(rightPoints[rightPoints.Length - 1], 0.1f);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < leftPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(leftPoints[i], leftPoints[i + 1]);
            Gizmos.DrawSphere(leftPoints[i], 0.1f);
        }
        Gizmos.DrawSphere(leftPoints[leftPoints.Length - 1], 0.1f);

        // draw the sampled right twin Bezier curve 
        Gizmos.color = Color.red;
        for (int i = 0; i < sampledFittedBezierR.Length - 1; i++)
        {
            Gizmos.DrawLine(sampledFittedBezierR[i], sampledFittedBezierR[i + 1]);
            Gizmos.DrawSphere(sampledFittedBezierR[i], 0.1f);
            Gizmos.DrawLine(sampledFittedBezierR[i], rightPoints[i]);
        }
        Gizmos.DrawSphere(sampledFittedBezierR[sampledFittedBezierR.Length - 1], 0.1f);

        // draw the sampled left twin Bezier curve
        Gizmos.color = Color.blue;
        for (int i = 0; i < sampledFittedBezierL.Length - 1; i++)
        {
            Gizmos.DrawLine(sampledFittedBezierL[i], sampledFittedBezierL[i + 1]);
            Gizmos.DrawSphere(sampledFittedBezierL[i], 0.1f);
            Gizmos.DrawLine(sampledFittedBezierR[i], rightPoints[i]);
        }
        Gizmos.DrawSphere(sampledFittedBezierL[sampledFittedBezierL.Length - 1], 0.1f);
    }

    /// <summary>
    /// Appriximates the Twin Bezier Curves left and right of the road to the equdistant lines left and right.
    /// </summary>
    public void ApproximateSecondaryCurves()
    {
        // measure distances
        float totalDiffRight = TotalDifference(rightBezTwin, rightPoints);
        float totalDiffLeft = TotalDifference(leftBezTwin, leftPoints);
        // Debug.Log($"the lenght of rightPoints: {rightPoints.Length}, the length of sampled Bez Curve right: {sampledFittedBezierR.Length}");
        // Debug.Log($"Epsilon is: {EPSILON}, errors are: right {totalDiffRight}, left {totalDiffLeft}");
        // approximate bez curves
        float step = 0.008f;
        if (totalDiffRight > EPSILON) ApproximateCurve(true, totalDiffRight, step, 10);
        if (totalDiffLeft > EPSILON) ApproximateCurve(false, totalDiffLeft, step, 10);
        // if (totalDiffRight < EPSILON) Debug.LogError($"BEFOREE ALL: error is alrady smaller that EPSILON: {totalDiffRight}");
        // if (totalDiffLeft < EPSILON) Debug.LogError($"BEFOREE ALL: error is alrady smaller that EPSILON: {totalDiffLeft}");
        // Update the BezierCurves and the sample points
        UpdateTwinBezierSamplePoints();
    }

    private float TotalDifference_bk(BezierCurve bez, Vector3[] points)
    {
        float samplingRate = 1f / (resolution - 1);  // same sampling rate as the equidistant lines
        float total = 0f;
        int valCount = 0;
        for (int i = 6; i < resolution - 6; i += 2)
        {
            Vector3 bezPoint = CurveUtility.EvaluatePosition(bez, i * samplingRate);
            float diff = Vector3.Distance(points[i], bezPoint);
            // float diff = (points[i] - bezPoint).magnitude;
            // Debug.Log($"Diff is: {diff}, points[{i}]: {points[i]}, and bezPoint: {bezPoint}");
            total += diff;
            valCount++;
        }
        return total / valCount;
        // return total;
    }

    private float TotalDifference(BezierCurve bez, Vector3[] points)
    {
        float samplingRate = 1f / (resolution - 1);  // same sampling rate as the equidistant lines
        float total = 0f;
        int valCount = 0;
        for (int i = 2; i < resolution - 2; i+=2)
        {
            Vector3 bezPoint_in1 = CurveUtility.EvaluatePosition(bez, (i - 1) * samplingRate);
            Vector3 bezPoint_i = CurveUtility.EvaluatePosition(bez, i * samplingRate);
            float diff = TriangleArea(bezPoint_in1, bezPoint_i, points[i]);
            total += diff;
            valCount++;
        }
        // return total / valCount;
        return total;
    }

    private float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
    }

    private void ApproximateCurve(bool right, float uncorrectedError, float step, int rounds)
    {
        // Make one end tangent shorter and calculate difference again
        // create a copy of the current knots
        Vector3 _p_0, _p_1, _p_2, _p_3;
        if (right) (_p_0, _p_1, _p_2, _p_3) = (pr_0, pr_1, pr_2, pr_3);
        else (_p_0, _p_1, _p_2, _p_3) = (pl_0, pl_1, pl_2, pl_3);

        Vector3[] points = right ? rightPoints : leftPoints;

        // data for tangent 1
        Vector3 dir1 = (_p_1 - _p_0).normalized;
        float length1 = (_p_1 - _p_0).magnitude;

        // data for tangent 2
        Vector3 dir2 = (_p_2 - _p_3).normalized;
        float length2 = (_p_2 - _p_3).magnitude;

        // correction step, for each iteration
        // float step = 0.004f;  // overall best value when using with circle approximation
        // Debug.Log($"step is: {step}");

        // Decide which tangent to correct in which direction
        // 16 Possibilities: tangent 1 +, tangent 1 -, tangent 2 +, tangent 2 -
        Vector3 _t1_p = _p_0 + (1 + step) * length1 * dir1;
        Vector3 _t1_n = _p_0 + (1 - step) * length1 * dir1;
        Vector3 _t2_p = _p_3 + (1 + step) * length2 * dir2;
        Vector3 _t2_n = _p_3 + (1 - step) * length2 * dir2;
        // expanding asymmetrically
        Vector3 _t1_p23rd = _p_0 + (1 + 2 * step / 3) * length1 * dir1;
        Vector3 _t1_p13rd = _p_0 + (1 + step / 3) * length1 * dir1;
        Vector3 _t1_n23rd = _p_0 + (1 - 2 * step / 3) * length1 * dir1;
        Vector3 _t1_n13rd = _p_0 + (1 - step / 3) * length1 * dir1;
        Vector3 _t2_p23rd = _p_3 + (1 + 2 * step / 3) * length2 * dir2;
        Vector3 _t2_p13rd = _p_3 + (1 + step / 3) * length2 * dir2;
        Vector3 _t2_n23rd = _p_3 + (1 - 2 * step / 3) * length2 * dir2;
        Vector3 _t2_n13rd = _p_3 + (1 - step / 3) * length2 * dir2;

        BezierCurve[] options = new BezierCurve[16];
        options[0] = new BezierCurve(_p_0, _t1_p, _p_2, _p_3);  // 1st tangent +
        options[1] = new BezierCurve(_p_0, _t1_n, _p_2, _p_3);  // 1st tangent -
        options[2] = new BezierCurve(_p_0, _p_1, _t2_p, _p_3);  // 2nd tangent +
        options[3] = new BezierCurve(_p_0, _p_1, _t2_n, _p_3);  // 2nd tangent -
        options[4] = new BezierCurve(_p_0, _t1_p, _t2_p, _p_3); // both tangents +
        options[5] = new BezierCurve(_p_0, _t1_n, _t2_n, _p_3); // both tangents -
        options[6] = new BezierCurve(_p_0, _t1_p, _t2_n, _p_3); // tangent 1 +, tangent 2 -
        options[7] = new BezierCurve(_p_0, _t1_n, _t2_p, _p_3); // tangent 1 -, tangent 2 +
        // assymmetric expansion, contraction
        options[8] = new BezierCurve(_p_0, _t1_p13rd, _t2_p23rd, _p_3); // tangent + 1/3 +, tangent + 2/3
        options[9] = new BezierCurve(_p_0, _t1_p23rd, _t2_p13rd, _p_3); // tangent + 2/3 +, tangent + 1/3
        options[10] = new BezierCurve(_p_0, _t1_p13rd, _t2_n23rd, _p_3); // tangent + 1/3 +, tangent - 2/3
        options[11] = new BezierCurve(_p_0, _t1_p23rd, _t2_n13rd, _p_3); // tangent + 2/3 +, tangent - 1/3
        options[12] = new BezierCurve(_p_0, _t1_n13rd, _t2_p23rd, _p_3); // tangent - 1/3 +, tangent + 2/3
        options[13] = new BezierCurve(_p_0, _t1_n23rd, _t2_p13rd, _p_3); // tangent - 2/3 +, tangent + 1/3
        options[14] = new BezierCurve(_p_0, _t1_n13rd, _t2_n23rd, _p_3); // tangent - 1/3 +, tangent - 2/3
        options[15] = new BezierCurve(_p_0, _t1_n23rd, _t2_n13rd, _p_3); // tangent - 2/3 +, tangent - 1/3


        int bestOption = -1;
        float lowestError = uncorrectedError;

        for (int i = 1; i < 16; i++)
        {
            float error = TotalDifference(options[i], points);
            // Debug.Log($"error in option {i} is: {error}");
            if (error < lowestError)
            {
                lowestError = error;
                bestOption = i;
            }
        }
        // Debug.Log($"best option is: {bestOption}, lowest error is: {lowestError}");

        // if non of the 4 options made improvements, make step smaller
        if (bestOption == -1)
        {
            if (rounds > 0)
            {
                step *= 0.5f;
                ApproximateCurve(right, uncorrectedError, step, rounds - 1);
            }
            // end approximation if rounds are used up
            return;
        }
        //  return;

        // obtain the Bezier Control knots with the intermediary knot (tangent) interpolated
        Vector3[] IPBezKnots = new Vector3[4];
        lowestError = uncorrectedError;
        Vector3[] originalBezNots = new[] { _p_0, _p_1, _p_2, _p_3 };
        switch (bestOption)
        {
            case 0:
                (IPBezKnots, lowestError) = InterpolateTangent10x(originalBezNots, 1, 1, step, dir1, length1, uncorrectedError, points);
                break;
            case 1:
                (IPBezKnots, lowestError) = InterpolateTangent10x(originalBezNots, 1, -1, step, dir1, length1, uncorrectedError, points);
                break;
            case 2:
                (IPBezKnots, lowestError) = InterpolateTangent10x(originalBezNots, 2, 1, step, dir2, length2, uncorrectedError, points);
                break;
            case 3:
                (IPBezKnots, lowestError) = InterpolateTangent10x(originalBezNots, 2, -1, step, dir2, length2, uncorrectedError, points);
                break;
            case 4:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, 1f, 1f, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 5:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, -1f, -1f, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 6:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, 1f, -1f, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 7:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, -1f, 1f, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 8:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, 1f / 3, 2f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 9:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, 2f / 3, 1f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 10:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, 1f / 3, -2f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 11:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, 2f / 3, -1f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 12:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, -1f / 3, 2f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 13:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, -2f / 3, 1f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 14:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, -1f / 3, -2f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            case 15:
                (IPBezKnots, lowestError) = InterPolateBothTangents10x(originalBezNots, -2f / 3, -1f / 3, step, dir1, dir2, length1, length2, uncorrectedError, points);
                break;
            default:
                break;
        }

        // apply the interpolated knots to the 
        if (right) (pr_0, pr_1, pr_2, pr_3) = (IPBezKnots[0], IPBezKnots[1], IPBezKnots[2], IPBezKnots[3]);
        else (pl_0, pl_1, pl_2, pl_3) = (IPBezKnots[0], IPBezKnots[1], IPBezKnots[2], IPBezKnots[3]);

        // TERMINAL CONDITION I: Approximation Accuracy is already reached
        if (uncorrectedError < EPSILON)
        {
            // Debug.LogError($"the uncorrected error: {uncorrectedError} is already smaller than {EPSILON}");
            return;
        }
        // // reset step:
        // step = 0.008f;
        // TERMINAL CONDITION II: recursion only, if number of rounds left is > 0
        if (rounds > 0)
        {
            rounds--;
            ApproximateCurve(right, lowestError, step, rounds);
        }
    }

    private (Vector3[], float) InterpolateTangent10x(Vector3[] knots, int tangent, int dirFactor, float step, Vector3 dir, float len, float uncorrectedError, Vector3[] points)
    {
        int iterations = 100;
        // index to start iterativ correction (either knot 0 or knot 3)
        int startIndex = tangent == 1 ? 0 : 3;

        float lowestError = uncorrectedError;
        int bestIFactor = 0;
        BezierCurve bez;
        // iteratively interpolate as long as it lowers the error
        for (int i = 0; i < iterations; i++)
        {
            knots[tangent] = knots[startIndex] + (1 + (dirFactor * i * step)) * len * dir;
            bez = new BezierCurve(knots[0], knots[1], knots[2], knots[3]);
            float error = TotalDifference(bez, points);
            // Debug.Log($"The error in {i} is: {error}");
            if (error < lowestError)
            {
                lowestError = error;
                bestIFactor = i;
            }
            else if (error > lowestError)
            {
                // Debug.Log($"Ended Interpolation after iteration {i}, and lowest error: {lowestError}");
                break;
            }
        }

        // recalculate the Bezier Curve
        knots[tangent] = knots[startIndex] + (1 + (dirFactor * bestIFactor * step)) * len * dir;

        // return the knots
        return (knots, lowestError);

    }

    private (Vector3[], float) InterPolateBothTangents10x(Vector3[] knots, float dirFactor1, float dirFactor2, float step, Vector3 dir1, Vector3 dir2, float len1, float len2, float uncorrectedError, Vector3[] points)
    {
        int iterations = 100;

        float lowestError = uncorrectedError;
        int bestIFactor = 0;
        BezierCurve bez;
        // iteratively interpolate as long as it lowers the error
        for (int i = 0; i < iterations; i++)
        {
            knots[1] = knots[0] + (1 + (dirFactor1 * i * step)) * len1 * dir1;
            knots[2] = knots[3] + (1 + (dirFactor2 * i * step)) * len2 * dir2;
            bez = new BezierCurve(knots[0], knots[1], knots[2], knots[3]);
            float error = TotalDifference(bez, points);
            // Debug.Log($"The error in {i} is: {error}");
            if (error < lowestError)
            {
                lowestError = error;
                bestIFactor = i;
            }
            else if (error > lowestError)
            {
                // Debug.Log($"Ended Interpolation after iteration {i}, and lowest error: {lowestError}");
                break;
            }
        }

        // recalculate the Bezier Curve
        knots[1] = knots[0] + (1 + (dirFactor1 * bestIFactor * step)) * len1 * dir1;
        knots[2] = knots[3] + (1 + (dirFactor2 * bestIFactor * step)) * len2 * dir2;

        // return the knots
        return (knots, lowestError);
    }

    public Vector3[] GetLeftPoints() => leftPoints;  // for debugging, sampledFittedBezierL;
    public Vector3[] GetRightPoints() => rightPoints;  // debugging, sampledFittedBezierR;

    public Vector3[] GetControlPoints() => new Vector3[] { pl_0, pl_1, pl_2, pl_3, pr_0, pr_1, pr_2, pr_3 };

    public Vector3 GetStartPoint() => pm_0;

    public Vector3 GetStartDirection() => pm_1 - pm_0;

    public BezierCurve GetBezierCurve() => bezierCurve;
}
