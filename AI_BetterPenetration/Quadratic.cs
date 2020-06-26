using System;
using UnityEngine;

namespace AI_BetterPenetration
{
    public static class Quadratic
    {
        // find the point along the line from VectorA to VectorB that is the same distance from the origin point as VectorC
        public static Vector3 SolveQuadratic(Vector3 vectorA, Vector3 vectorB, Vector3 vectorC, float distOriginToB, float distOriginToC)
        {
            float a = Vector3.Distance(vectorA, vectorC);
            float b = Vector3.Distance(vectorA, vectorB);
            float c = Vector3.Distance(vectorC, vectorB);
            double angleC = 0;
            if (a != 0 && b != 0)
                angleC = Math.Acos(((a * a) + (b * b) - (c * c)) / (2 * a * b));

            angleC = Math.PI - angleC;

            double quadB = -2 * distOriginToB * Math.Cos(angleC);
            double quadC = (distOriginToB * distOriginToB) - (distOriginToC * distOriginToC);
            double quadD = (quadB * quadB) - (4 * quadC);
            double vectorAtoBtravel = 0;
            if (quadD >= 0)
                vectorAtoBtravel = (-quadB + Math.Sqrt(quadD)) / 2;

            return Vector3.LerpUnclamped(vectorA, vectorB, (float)vectorAtoBtravel / b);
        }

    }
}
