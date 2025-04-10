using UnityEngine;
using System;
using UnityEngine.Splines;

public class BezierRoadGeometry
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

    public static float SegmentLength(BezierRoadState state, float strechFactor)
    {
        Vector3[] bKnots = CircularCubicBezierKnots(0, state.radius, state.segmentCount);
        BezierCurve bezierCurve = new BezierCurve(bKnots[0], bKnots[1], bKnots[2], bKnots[3]);
        float length = CurveUtility.CalculateLength(bezierCurve) * strechFactor;
        return length;
    }

    // public static float RoadLength(BezierRoadState state)
    // {
    //     float totalLength = state.segmentCount * SegmentLength(state);
    //     return totalLength;
    // }

    public static void CalculateRoadCornerPositions(BezierRoadState state, float[] deviations, int[] segPerEdge, int cornerCount, float segLen)
    {
        // copy frequently used state variables
        float radius = state.radius;
        int[] points = state.primaryScatterPoints;
        Vector3[][] knots = state.bezierKnots;

        // SKIP THE FIRST CORNER, since its never deviated
        // only adjust its neigbours
        Vector3[] neighbours0 = CurveNeighbours(0f, radius);
        knots[knots.Length - 1][2] = neighbours0[0];
        knots[0][1] = neighbours0[1];

        // calculate the angle between the the deviated corners
        // USING Law of Cosinus: Cos(gamma) = (a^2 + b^2 + c^2) / (2 * a * b)
        // accumulated angle
        float gamma = 0f;
        for (int i = 1; i < points.Length - 1; i++)
        {
            // Debug.Log($"i is: {i} points len is {points.Length}, and deviations len is {deviations.Length}, and points[i]={points[i]}");
            float _radius_i = deviations[points[i]] * radius;
            float _radius_im1 = deviations[points[i - 1]] * radius;
            float cos_gamma = (Mathf.Pow(_radius_i, 2f) + Mathf.Pow(_radius_im1, 2f) - Mathf.Pow(segPerEdge[i] * segLen, 2f))
            / (2 * _radius_i * _radius_im1);

            gamma += Mathf.Acos(cos_gamma);

            // calculate the corner point
            float _x = _radius_i * Mathf.Cos(gamma);
            float _z = _radius_i * Mathf.Sin(gamma);
            Vector3 corner = new(_x, 0, _z);

            knots[points[i]][0] = corner;
            knots[points[i] - 1][3] = corner;

            // ADJUST NEIGHBOURING INTERMEDIATE KNOTS
            Vector3[] neighbours = CurveNeighbours(gamma, _radius_i);

            // update movement to intermediary knots
            knots[points[i]][1] = neighbours[1];
            knots[points[i] - 1][2] = neighbours[0];
        }
        // CALCULATE THE LAST cornerpoint from the last and second-last edges
        // distance between first corner point and third last cornerpoint
        Vector3 _dVec = knots[0][0] - knots[points[cornerCount - 2]][0];
        float _distance = _dVec.magnitude;
        Vector3 _dirVec = _dVec.normalized;
        // calculate beta with Law of Cosinus
        float cos_beta = (Mathf.Pow(segPerEdge[cornerCount - 2] * segLen, 2f) - Mathf.Pow(segPerEdge[cornerCount - 1] * segLen, 2f) +
        Mathf.Pow(_distance, 2f)) / (2 * segPerEdge[cornerCount - 2] * segLen * _distance);
        float beta = Mathf.Acos(cos_beta) * (-1f);

        // PLACE THE LAST CORNER in the direction of the distance vector rotated by beta
        // Calculate rotation
        float sin = Mathf.Sin(beta);
        float cos = Mathf.Cos(beta);

        // rotation in x-z-plane
        Vector3 rotatedDir = new(
            _dirVec.x * cos - _dirVec.z * sin,
            0f,
            _dirVec.x * sin + _dirVec.z * cos
        );

        // calculate rotated offset
        Vector3 offset = rotatedDir.normalized * segPerEdge[cornerCount - 2] * segLen;
        Vector3 finalCorner = knots[points[cornerCount - 2]][0] + offset;

        // save to the corresponding knots
        knots[points[cornerCount - 1] - 1][3] = knots[points[cornerCount - 1]][0] = finalCorner;

        // ADJUST NEIGHBOURING Intermediate knots
        // calculate the angle between the final corner and the first curve segment's start knot
        float finalRadius = (new Vector3(0f, 0f, 0f) - finalCorner).magnitude;
        float cos_gamma_fin = (Mathf.Pow(finalRadius, 2f) + Mathf.Pow(radius, 2f) - Mathf.Pow(segPerEdge[cornerCount - 1] * segLen, 2f))
        / (2 * finalRadius * radius);
        float gamma_fin = 2 * Mathf.PI - Mathf.Acos(cos_gamma_fin);

        Vector3[] neighFin = CurveNeighbours(gamma_fin, finalRadius);
        knots[points[cornerCount - 1] - 1][2] = neighFin[0];
        knots[points[cornerCount - 1]][1] = neighFin[1];
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
    public static void AdjustKnots(BezierRoadState state)
    {
        // create array for index pairs (start and end)
        int[][] indexPairs = BezUtils.IndexPairsFromPrimaryS1(state);

        for (int i = 0; i < state.primaryScatterPoints.Length; i++)
        {
            // Interpolate(indexPairs[i][0], indexPairs[i][1], state.bezierKnots);
            InterpolateOld(indexPairs[i][0], indexPairs[i][1], state.bezierKnots);
        }

        // create secondary scattering
        // ApplySecondaryScattering(state.bezierKnots, deviations);

        for (int i = 0; i < state.primaryScatterPoints.Length; i++)
        {
            // ensure continuity
            EnforceC1(indexPairs[i][0], indexPairs[i][1], state.bezierKnots);
        }

        // update all Bezier segment components
        for (int i = 0; i < state.segmentCount; i++) state.bezierCurves[i].ApplyMainBezierKnots(state.bezierKnots[i]);
    }

    /// <summary>
    /// Interpolates the position of the Bezier control knots of the given curves.
    /// <br/> Scheme:
    /// \[start: {1}{2}{3}]\[start + 1: (0)(1)(2)(3)]\ ... \[end - 2: (0)(1)(2)(3)]\[end - 1: {0}{1}{2}]\ 
    /// </summary>
    /// <param name="start">index of start curve</param>
    /// <param name="end">index of end curve still included (penultimate)</param>
    /// <param name="bezierKnots">mian Bezier controll knots for all segments</param>
    public static void Interpolate(int start, int end, Vector3[][] bezierKnots)
    {
        // DEBUG
        // Debug.LogError($"The indices: {start} and {end}");
        // calculate the number of full curve to be adjusted (subtract the incomplete curves at the end, see scheme)
        int segments = end - start - 1;
        // Debug.Log($"the knotCount is: {segments}");

        // define the sampling factor
        float sampling = 1f / (segments * 3 + 3);  // add 3 and 2 for {extra knots} at ends

        // calculate vector between start[0] and end[0] (or end-1[3])
        Vector3 distance = bezierKnots[end - 1][3] - bezierKnots[start][0];

        // calculate the knots 2 and 3 from the start curve
        bezierKnots[start][1] = bezierKnots[start][0] + 1 * sampling * distance;
        bezierKnots[start][2] = bezierKnots[start][0] + 2 * sampling * distance;
        bezierKnots[start][3] = bezierKnots[start][0] + 3 * sampling * distance;

        // iterate over the bezier knots
        for (int i = 0; i < segments; i++)
        {
            // calculate the new position of the knots
            bezierKnots[(start + 1) + i][0] = bezierKnots[start][0] + (3 + 3 * i) * sampling * distance;
            bezierKnots[(start + 1) + i][1] = bezierKnots[start][0] + (4 + 3 * i) * sampling * distance;
            bezierKnots[(start + 1) + i][2] = bezierKnots[start][0] + (5 + 3 * i) * sampling * distance;
            bezierKnots[(start + 1) + i][3] = bezierKnots[start][0] + (6 + 3 * i) * sampling * distance;
        }

        // calculate the knots 0 and 1 from the curve [end - 1]
        bezierKnots[end - 1][0] = bezierKnots[start][0] + (3 * segments) * sampling * distance;
        bezierKnots[end - 1][1] = bezierKnots[start][0] + (1 + 3 * segments) * sampling * distance;
        bezierKnots[end - 1][2] = bezierKnots[start][0] + (2 + 3 * segments) * sampling * distance;
        // Debug.LogError($"Interpolating from {start} to {end}");

    }

    /// <summary>
    /// Interpolates the position of the Bezier control knots of the given curves.
    /// <br/> Scheme:
    /// \[start: {2}{3}]\[start + 1: (0)(1)(2)(3)]\ ... \[end - 2: (0)(1)(2)(3)]\[end - 1: (0){1}]\ 
    /// </summary>
    /// <param name="start">index of start curve</param>
    /// <param name="end">index of end curve still included (penultimate)</param>
    /// <param name="bezierKnots">mian Bezier controll knots for all segments</param>
    public static void InterpolateOld(int start, int end, Vector3[][] bezierKnots)
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



    public static void ApplySecondaryScattering(Vector3[][] bezierKnots, float[] deviations)
    {
        // itearate over the bezierKnot groups (Bezier curve representations) and deviate the intermediary knots
        for (int i = 0; i < bezierKnots.Length; i++)
        {
            Vector3 normal = new(0f, 1f, 0f);

            // 1ST INTERMEDIARY KNOT
            float deviation1 = -1 * deviations[BezUtils.CircularIndex(i - 1, bezierKnots.Length)] / 2;

            // calculate the normal vector of the curve
            Vector3 curvedir1 = (bezierKnots[i][1] - bezierKnots[i][0]).normalized;
            Vector3 deviatedVec1 = Vector3.Cross(normal, curvedir1).normalized * deviation1;

            // update the first intermediary knot
            bezierKnots[i][1] = bezierKnots[i][1] + deviatedVec1;

            // 2nd INTERMEDIARY KNOT
            // adjust the deviation
            float deviation2 = deviations[i] / 2;

            // calculate the normal vector of the curve
            Vector3 curvedir2 = (bezierKnots[i][3] - bezierKnots[i][2]).normalized;
            Vector3 deviatedVec2 = Vector3.Cross(normal, curvedir2).normalized * deviation2;

            // update the last intermediary knot
            bezierKnots[i][2] = bezierKnots[i][2] + deviatedVec2;
        }

    }

    public static void EnforceC1(int start, int end, Vector3[][] knots)
    {
        // mirror the intermediary knot of the previous segment
        Vector3 distanceVector = knots[start][0] - knots[BezUtils.CircularIndex(start - 1, knots.Length)][2];
        knots[start][1] = knots[start][0] + distanceVector;
    }

    public static Vector3[] CurveNeighbours(float gamma, float radius)
    {
        // difference
        float diff = 0.06f;

        // neighbour n - 1
        float x_0 = radius * Mathf.Cos((2 * Mathf.PI + gamma - diff) % (2 * Mathf.PI));
        float z_0 = radius * Mathf.Sin((2 * Mathf.PI + gamma - diff) % (2 * Mathf.PI));

        Vector3 p_0 = new(x_0, 0, z_0);

        // neighbour n + 1
        float x_2 = radius * Mathf.Cos(gamma + diff);
        float z_2 = radius * Mathf.Sin(gamma + diff);
        Vector3 p_2 = new(x_2, 0, z_2);

        return new Vector3[] { p_0, p_2 };
    }

    public static void RelaxCurves(BezierRoadState state)
    {
        Vector3[][] knots = state.bezierKnots;
        int size = state.segmentCount;
        float minAngle = 120f;  // 120°
        // iterate over curves, measure the angles of the tangents and correct them is they are too small
        for (int i = 0; i < size; i++)
        {
            Vector3 startTangent = knots[i][1] - knots[i][0];
            Vector3 endTangent = knots[i][2] - knots[i][3];
            float angle = Vector3.Angle(startTangent, endTangent);
            if (angle < minAngle)
            {
                Debug.LogError($"the angle in segment {i} is {angle}. Relaxing...");
                // move the intermediary knots of the corresponding tangents half way to the middle line
                // and then andjust the intermediary knots on the other side of the start / end knot

                float magStart = startTangent.magnitude;
                float magEnd = endTangent.magnitude;

                Vector3 dir = (knots[i][3] - knots[i][0]).normalized;
                Vector3 startProj = knots[i][0] + dir * magStart;
                Vector3 endProj = knots[i][3] + -1f * magEnd * dir;

                Vector3 startTanCor = Vector3.Lerp(knots[i][1], startProj, 0.5f);
                Vector3 endTanCor = Vector3.Lerp(knots[i][2], endProj, 0.5f);

                knots[i][1] = startTanCor;
                knots[i][2] = endTanCor;

                Vector3 diffStart = startTanCor - knots[i][0];
                Vector3 diffEnd = knots[i][3] - endTanCor;

                int prev = BezUtils.CircularIndex(i - 1, size);
                int next = BezUtils.CircularIndex(i + 1, size);
                knots[prev][2] = knots[i][0] - diffStart;
                knots[next][1] = diffEnd + knots[i][3];
            }

        }
    }
}