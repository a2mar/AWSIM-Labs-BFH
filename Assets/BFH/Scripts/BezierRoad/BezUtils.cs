public class BezUtils
{
    /// <summary>
    /// calculates and return the Bezier curve index pairs for all the intervalls between the Bezier knots targeted with the randomization.
    /// The calclated indeces are always the the index of the Bezier curve whose first control knot has been randomized,
    /// except for the the first curve segment. It the intervall ends at the first segment, segmentCount instead of 0 is returned.
    /// </summary>
    /// <returns>array with index pairs</returns>
    public static int[][] IndexPairsFromPrimaryS(BezierRoadState state)
    {
        // index pair array
        int[][] indexPairs = new int[state.primaryScatterPoints.Length + 1][];


        // calculate the index pairs (strting from the curve )
        for (int i = 0; i <= state.primaryScatterPoints.Length; i++)
        {
            // special case first intervall (between segment 0's start knot and first randomly scattered segments's end knot) 
            if (i == 0)
            {
                indexPairs[i] = new int[] { 0, state.primaryScatterPoints[i] + 1 };
            }
            // special case last interval (between last randomly scattered segments's end knot and the last segment's end knot)
            else if (i == state.primaryScatterPoints.Length)
            {
                indexPairs[i] = new int[] { CircularIndex(state.primaryScatterPoints[i - 1] + 1, state.segmentCount), state.segmentCount };
            }
            // normal case (between two subsequent randomized end knots)
            else
            {
                indexPairs[i] = new int[] { state.primaryScatterPoints[i - 1] + 1, state.primaryScatterPoints[i] + 1 };
            }

        }

        return indexPairs;
    }


    public static int[][] IndexPairsFromPrimaryS1(BezierRoadState state)
    {
        // copy state variable
        int[] points = state.primaryScatterPoints;
        // index pair array
        int[][] indexPairs = new int[points.Length][];


        // calculate the index pairs (strting from the curve )
        for (int i = 0; i < points.Length; i++)
        {
            // // special case first intervall (between segment 0's start knot and first randomly scattered segments's end knot) 
            // if (i == 0)
            // {
            //     indexPairs[i] = new int[] { 0, state.primaryScatterPoints[i] + 1 };
            // }
            // // special case last interval (between last randomly scattered segments's end knot and the last segment's end knot)
            // else if (i == state.primaryScatterPoints.Length)
            // {
            //     indexPairs[i] = new int[] { CircularIndex(state.primaryScatterPoints[i - 1] + 1, state.segmentCount), state.segmentCount };
            // }
            // // normal case (between two subsequent randomized end knots)
            // else
            // {
            //     indexPairs[i] = new int[] { state.primaryScatterPoints[i - 1] + 1, state.primaryScatterPoints[i] + 1 };
            // }
            if (i == points.Length - 1) indexPairs[i] = new int[] { points[i], state.segmentCount };
            else indexPairs[i] = new int[] { points[i], points[CircularIndex(i + 1, points.Length)]};
            
        }

        return indexPairs;
    }

    /// <summary>
    /// Calculates the circular index for a non-circular bounded indexed data-type for a desired index.
    /// </summary>
    /// <param name="index">the desired index</param>
    /// <param name="size">the size of the array</param>
    /// <returns>corrected index</returns>
    public static int CircularIndex(int index, int size)
    {
        return (size + index) % size;
    }
}