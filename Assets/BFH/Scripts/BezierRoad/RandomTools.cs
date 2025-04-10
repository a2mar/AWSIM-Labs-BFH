using System;
using System.Linq;
using UnityEngine;

using static BezierRoadManager;

public class RandomTools
{
    public static int CornerCount(BezierRoadState state, RoadType roadType)
    {
        int gap = roadType == RoadType.Simple ? 8 : 4;
        int cornerCount = state.segmentCount / gap;
        if (cornerCount < 4) cornerCount = 4;
        return cornerCount;
    }

    public static int[] SegmentsPerEdge(BezierRoadState state, RoadType roadType)
    {
        int gap = roadType == RoadType.Simple ? 8 : 4;
        int cornerCount = state.segmentCount / gap;
        if (cornerCount < 4) cornerCount = 4;
        Debug.Log($"the cornerCount is :{cornerCount}");
        int[] segPerEdge = new int[cornerCount];
        state.primaryScatterPoints = new int[cornerCount];
        state.primaryScatterPoints[0] = 0;
        for (int i = 0; i < cornerCount; i++)
        {
            segPerEdge[i] = gap;
            state.primaryScatterPoints[i] = i * gap;
        }

        return segPerEdge;
    }

    public static float[] Deviations(BezierRoadState state)
    {
        float[] deviations = new float[state.randomNumbers.Length];
        for (int i = 0; i < state.randomNumbers.Length; i++)
        {
            deviations[i] = (100.0f + state.randomNumbers[i]) / 100.0f;
        }
        return deviations;
    }


    /// <summary>
    /// Randomly chooses the indices of the bezier curve segments to be randomized with primary randomization.
    /// The amount of randomized curve segments depends on the road type, as well as on the segment count
    /// </summary>
    public static void DetermineRandomBezierSegments(BezierRoadState state, RoadType roadType)
    {
        // define gap (# non-rand. segments) between two randomized segments 
        int gap = roadType == RoadType.Simple ? 8 : 4;

        // calculate how many points have primary randomization
        int amountOfPoints = (state.segmentCount / gap) - 1;  // -1 to prevent last segment to have scattering
        // if amount of points is 1, double the gap forcibly
        if (amountOfPoints < 2)
        {
            // decrease the gap and amount of points
            gap /= 2;
            amountOfPoints = (state.segmentCount / gap) - 1;
        }
        state.primaryScatterPoints = new int[amountOfPoints];
        // primaryScatterPoints = new int[] { 3, 11, 19 };

        System.Random random = new System.Random();
        int start = Math.Max((int)(random.NextDouble() * (gap - 1)), 3);

        for (int i = 0; i < amountOfPoints; i++)
        {
            // check 
            state.primaryScatterPoints[i] = i * gap + start;
            // primaryScatterPoints[i] = i * gap + 3;
            // Debug.Log($"the scatter point determined is: {primaryScatterPoints[i]}");
        }

    }

    /// <summary>
    /// Generates the random deviations for all road segments except the first
    /// TODO: adjust and rename according to new functionality 
    /// </summary>
    public static void GenerateRandomNumbers(BezierRoadState state, float scatteringRange, float secundaryScatteringRange)
    {
        // copy read-only state
        int segmentCount = state.segmentCount;
        int[] primaryScatterPoints = state.primaryScatterPoints;

        state.randomNumbers = new float[segmentCount];
        // no randomization on the last segment's end point
        state.randomNumbers[0] = 0;
        state.randomNumbers[segmentCount - 1] = 0;
        for (int i = 1; i < segmentCount - 1; i++)
        {
            // determine if the segment has PRIMARY randomization
            bool primaryRandomization = state.primaryScatterPoints.Contains(i);

            // determine if segment has SECONDARY randomization (no randomization on segments neighbouring primarily randomized segments) 
            // modulus for array boundary safety
            bool secondaryRandomization =
                !primaryScatterPoints.Contains(BezUtils.CircularIndex(i - 1, segmentCount)) ||
                !primaryScatterPoints.Contains(i) ||
                !primaryScatterPoints.Contains(BezUtils.CircularIndex(i + 1, segmentCount));

            System.Random random = new System.Random();

            // crete randomization based on scattering range
            if (primaryRandomization)
            {
                // generate random number for primary scattering
                state.randomNumbers[i] = (float)(random.NextDouble() * 2.0 * scatteringRange - scatteringRange);
            }
            else if (secondaryRandomization)
            {
                // generate random number in range of secondary scattering
                state.randomNumbers[i] = (float)(random.NextDouble() * 2.0 * secundaryScatteringRange - secundaryScatteringRange);
            }
            else
            {
                // no randomization
                state.randomNumbers[i] = 0;
            }
        }
    }
}