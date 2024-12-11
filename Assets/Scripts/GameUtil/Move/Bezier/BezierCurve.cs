using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public static class BezierCurve {

    public static void EvaluateBezierWithAngle(
        float3 P0, float3 P3, float angle1, float angle2, float length1, float length2,
        int NumPointsBetweenEachSegment, ref NativeArray<float3> OutPoints, out float Length) {
        // Path length.  
        Length = 0f;

        // 计算控制点 P1 和 P2 的位置  
        float3 direction = normalize(P3 - P0); // 起点到终点的方向向量  

        // 计算 P1 的位置  
        float3 perpendicular1 = new float3(-direction.y, direction.x, direction.z); // 垂直方向  
        float3 rotatedDirection1 = rotateVector(direction, perpendicular1, angle1); // 根据角度旋转  
        float3 P1 = P0 + rotatedDirection1 * length1; // 控制点 1 的位置  

        // 计算 P2 的位置  
        float3 perpendicular2 = new float3(-direction.y, direction.x, direction.z); // 垂直方向  
        float3 rotatedDirection2 = rotateVector(direction, perpendicular2, angle2); // 根据角度旋转  
        float3 P2 = P3 - rotatedDirection2 * length2; // 控制点 2 的位置  

        // 调用原始 EvaluateBezier 函数  
        EvaluateBezier(P0, P1, P2, P3, NumPointsBetweenEachSegment, ref OutPoints, out Length);
    }

    // 旋转向量函数  
    private static float3 rotateVector(float3 vector, float3 axis, float angle) {
        // 使用 Rodrigues' rotation formula 旋转向量  
        float cosTheta = Mathf.Cos(angle);
        float sinTheta = Mathf.Sin(angle);

        return vector * cosTheta + cross(axis, vector) * sinTheta + axis * dot(axis, vector) * (1 - cosTheta);
    }

    /// <summary>
    /// The Unreal One (Evaluate!)
    /// </summary>
    /// <param name="P0"> Start Point </param>
    /// <param name="P1"> Control Point 1 </param>
    /// <param name="P2"> Control Point 2 </param>
    /// <param name="P3"> End Point </param>
    /// <param name="Length"></param>
    /// <returns></returns>
    public static void EvaluateBezier(float3 P0, float3 P1, float3 P2, float3 P3, int NumPointsBetweenEachSegment, ref NativeArray<float3> OutPoints, out float Length) {
        // Path length.
        Length = 0f;

        // Result
        if (!checkVector3(P0) || !checkVector3(P1) || !checkVector3(P2) || !checkVector3(P3)) {
            //ActDebug.Other_UD.I?.Log("Control Point Error");
            return;
        }

        float q = 1.0f / (NumPointsBetweenEachSegment - 1); // q is dependent on the number of GAPS = POINTS-1

        // coefficients of the cubic polynomial that we're FDing -
        float3 a = P0;
        float3 b = 3 * (P1 - P0);
        float3 c = 3 * (P2 - 2 * P1 + P0);
        float3 d = P3 - 3 * P2 + 3 * P1 - P0;

        // initial values of the poly and the 3 diffs -
        float3 S = a;                                  // the poly value
        float3 U = b * q + q * q * c + q * q * q * d;  // 1st order diff (quadratic)
        float3 V = 2 * q * q * c + 6 * q * q * q * d;  // 2nd order diff (linear)
        float3 W = 6 * q * q * q * d;                  // 3rd order diff (constant)

        float3 OldPos = P0;
        OutPoints[0] = P0;  // first point on the curve is always P0.

        for (int i = 1; i < NumPointsBetweenEachSegment; ++i) {
            // calculate the next value and update the deltas
            S += U;         // update poly value
            U += V;         // update 1st order diff value
            V += W;         // update 2st order diff value
                            // 3rd order diff is constant => no update needed.

            // Update Length.
            Length += distance(S, OldPos);
            OldPos = S;

            OutPoints[i] = S;
        }

        // Return path as experienced in sequence (linear interpolation between points).
        //return OutPoints;
    }

    static bool checkVector3(float3 one) {
        return !isnan(one.x) && !isnan(one.y) && !isnan(one.z);
    }
}