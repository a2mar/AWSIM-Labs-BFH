using UnityEngine;
using System;

public class BezierRoadGemetry
{
    /// <summary>
    /// Creates Bezier Knots for all the road segments and applies them to the corresponding Bezier Curve Groups
    /// </summary>
    /// <param name="state">Bezier Road State</param>
    public static void InitializeBezierKnots(BezierRoadState state)
    {
        // allocate the Bezier Knots array
        state.bezierKnots = new Vector3[state.segmentCount][];

        // allocate each row to hold 4 Vector3 elements (cubic Bezier curve's control knots)
        for (int i = 0; i < state.segmentCount; i++)
        {
            // bezierKnots[i] = CubicBezierKnots(previous);
            state.bezierKnots[i] = CircularCubicBezierKnots(i, state.radius, state.segmentCount);
            // apply to the corresponding Bezier Curve Components
            state.bezierCurves[i].ApplyMainBezierKnots(state.bezierKnots[i]);
        }
    }


    /// <summary>
    /// Calculates the 4 control knots of a Bezier curve for a predifined angle and radius for a circular race track.
    /// </summary>
    /// <param name="segment">the index of the road segment for which the Bezier curve knots are calculated</param>
    /// <returns></returns>
    public static Vector3[] CircularCubicBezierKnots(int segment, float radius, int segmentCount)
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
    /// Apply the randomization to the Bezier end knots
    /// </summary>
    public static void ApplyRandomToBezierKnots(BezierRoadState state)
    {
        // copy state
        int segmentCount = state.segmentCount;
        
        System.Random random = new System.Random();

        for (int i = 0; i < state.bezierKnots.Length; i++)
        {
            // calculate the displacement factor from random deviations
            float deviation = (100.0f + state.randomNumbers[i]) / 100.0f;

            // add random deviation to end knot of segment
            state.bezierKnots[i][3] = state.bezierKnots[i][3] * deviation;

            // also add deviation to the neighbouring knots
            state.bezierKnots[i][2] = state.bezierKnots[i][2] * deviation;
            state.bezierKnots[BezUtils.CircularIndex(i + 1, segmentCount)][1] = state.bezierKnots[BezUtils.CircularIndex(i + 1, segmentCount)][1] * deviation;

            // the start knot must mirror the end knot of the previous segment
            state.bezierKnots[i][0] = state.bezierKnots[BezUtils.CircularIndex(i - 1, segmentCount)][3];
        }
    }

    /// <summary>
    /// make Bezier Curves adjustment between primarily scattered Bezier knots
    /// </summary>
    public static void AdjustKnotsWithScattering(BezierRoadState state)
    {
        // create array for index pairs (start and end)
        int[][] indexPairs = BezUtils.IndexPairsFromPrimaryS(state);
        for (int i = 0; i < indexPairs.Length; i++)
        {
            //Debug.Log($"the indexpair i={i}, has {indexPairs[i][0]} and {indexPairs[i][1]}");
        }

        for (int i = 0; i <= state.primaryScatterPoints.Length; i++)
        {
            Interpolate(indexPairs[i][0], indexPairs[i][1], state.bezierKnots);
        }

        // update all Bezier segment components
        for (int i = 0; i < state.segmentCount; i++) state.bezierCurves[i].ApplyMainBezierKnots(state.bezierKnots[i]);
    }

    /// <summary>
    /// Interpolates the position of the Bezier control knots of the given curves.
    /// <br/> Scheme:
    /// \[start: {2}{3}]\[start + 1: (0)(1)(2)(3)]\ ... \[end - 2: (0)(1)(2)(3)]\[end - 1: (0){1}]\ 
    /// </summary>
    /// <param name="start">index of start curve</param>
    /// <param name="end">index of end curve still included (penultimate)</param>
    /// <param name="bezierKnots">mian Bezier controll knots for all segments</param>
    public static void Interpolate(int start, int end, Vector3[][] bezierKnots)
    {
        // calculate the number of full curve to be adjusted (subtract the incomplete curves at the end, see scheme)
        int segments = end - start - 2;
        // Debug.Log($"the knotCount is: {segments}");

        // define the sampling factor
        float sampling = 1f / (segments * 3 + 2 + 1);  // add 2 for {extra knots} at ends, add 1 for

        // calculate vector between start and end
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
}