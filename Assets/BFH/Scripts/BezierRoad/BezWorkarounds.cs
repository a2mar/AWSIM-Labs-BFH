using UnityEngine;
using UnityEngine.Splines;

public class BezWorkarounds
{
        /// <summary>
    /// WORKAROUND for occasions when the last corner point cannot be properly calculated due to the large distance.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="stretchFactor"></param>
    /// <returns></returns>
    public static bool RoadUnbroken(BezierRoadState state, float stretchFactor)
    {
        Vector3[][] knots = state.bezierKnots;
        for (int i = 0; i < knots.Length; i++)
        {
            // Debug.LogError("HEREHRHE!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            BezierCurve currentCurve = new BezierCurve(knots[i][0], knots[i][1], knots[i][2], knots[i][3]);
            float currentLength = CurveUtility.CalculateLength(currentCurve);
            float targetLength = BezierRoadGeometry.SegmentLength(state, stretchFactor);
            float ratio = currentLength / targetLength;
            // Debug.LogError($"{i}the currentLength is: {currentLength}, target length is: {targetLength}");
            if (ratio > 1.5f) return false;
        }
        // Debug.Log("PASSED THE TEST");
        return true;
    }

    /// <summary>
    /// WORKAROUND the fix NaN if the road cannot be calculated iteratively.
    /// Checks if any vector components are NaN.
    /// </summary>
    /// <param name="state"></param>
    /// <returns>true if any vector component is NaN</returns>
    public static bool RoadVectorsCorrupt(BezierRoadState state)
    {

        for (int i = 0; i < state.segmentCount; i++)
        {
            if (float.IsNaN(state.bezierCurves[state.segmentCount - 1].GetLeftPoints()[1].x))
            {
                Debug.LogError("Found Corrupt vectors");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// WORKAROUND for prevention of beak shaped last corner.
    /// They sould have at least the distance of two segments.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="segLen"></param>
    /// <returns>True if two corners with distance 2 are too close</returns>
    public static bool CornersTooClose(BezierRoadState state, float segLen)
    {
        Vector3[][] knots = state.bezierKnots;
        int[] points = state.primaryScatterPoints;
        int size = points.Length;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                int index1 = BezUtils.CircularIndex(i - 1, size);
                int index2 = BezUtils.CircularIndex(i + j, size);
                float distance = Vector3.Distance(
                        knots[points[index1]][0],
                        knots[points[index2]][0]
                    );
                float ratio = distance / segLen;
                if (ratio < 3f)
                {
                    return true;
                }
            }
        }
        return false;
    }
    /// <summary>
    /// determine the angle between neighbouring Bezier Curves
    /// </summary>
    /// <param name="state"></param>
    /// <returns>True if the angle between curves is less than 120 degrees</returns>
    public static bool TangentsTooCLose(BezierRoadState state)
    {
        Vector3[][] knots = state.bezierKnots;
        int size = state.segmentCount;

        for (int i = 0; i < size; i++)
        {
            int index_neg_1 = BezUtils.CircularIndex(i - 1, size);
            Vector3 tangent1 = knots[index_neg_1][3] - knots[index_neg_1][2];
            Vector3 tangent2 = knots[i][0] - knots[i][1];
            float angle = Vector3.Angle(tangent1, tangent2);
            if (angle < 120f)
            {
                Debug.LogError($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!¨The angle is smaller at {i}!!!!!!!!!!!!");
            }

        }

        return false;
    }
}