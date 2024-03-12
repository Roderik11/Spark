using System;
using SharpDX;

namespace Spark.Noise
{
    public static partial class MathHelper
    {
        public const float Pi = (float)Math.PI;

        public const float TwoPi = (float)(Math.PI * -2);

        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(Math.Min(value, max), min);
        }

        public static float Clamp(float value, float min, float max)
        {
            return Math.Max(Math.Min(value, max), min);
        }

        public static void Swap(ref float a, ref float b)
        {
            float tmp = 0F;
            tmp = a;
            a = b;
            b = tmp;
        }

        public static float Scale(float value, float inLow, float inHigh, float outLow, float outHigh)
        {
            if (inLow > inHigh)
            {
                Swap(ref inLow, ref inHigh);

                if (value > inHigh)
                    value = inHigh;

                if (value < inLow)
                    value = inLow;

                if (inLow == inHigh)
                    return outLow;

                Swap(ref inLow, ref inHigh);

                if (outHigh > outLow) //normal
                    return ((value - inLow) / (inHigh - inLow)) * (outHigh - outLow) + outLow;
                else //inverse
                    return outLow - ((outLow - outHigh) * ((value - inLow) / (inHigh - inLow)));
            }
            else
            {
                if (value > inHigh)
                    value = inHigh;
                if (value < inLow)
                    value = inLow;
                if (inLow == inHigh)
                    return outLow;

                if (outHigh > outLow) //normal
                    return ((value - inLow) / (inHigh - inLow)) * (outHigh - outLow) + outLow;
                else //inverse
                    return outLow - ((outLow - outHigh) * ((value - inLow) / (inHigh - inLow)));
            }
        }
        public static T[] Array<T>(T value, int count)
        {
            T[] result = new T[count];
            for (int i = 0; i < count; i++)
                result[i] = value;

            return result;
        }

        /// <summary>
        ///   Converts revolutions to degrees.
        /// </summary>
        /// <param name = "revolution">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RevolutionsToDegrees(float revolution)
        {
            return revolution * 360.0f;
        }

        /// <summary>
        ///   Converts revolutions to radians.
        /// </summary>
        /// <param name = "revolution">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RevolutionsToRadians(float revolution)
        {
            return revolution * MathHelper.TwoPi;
        }

        /// <summary>
        ///   Converts revolutions to gradians.
        /// </summary>
        /// <param name = "revolution">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RevolutionsToGradians(float revolution)
        {
            return revolution * 400.0f;
        }

        /// <summary>
        ///   Converts degrees to revolutions.
        /// </summary>
        /// <param name = "degree">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float DegreesToRevolutions(float degree)
        {
            return degree / 360.0f;
        }

        /// <summary>
        ///   Converts degrees to radians.
        /// </summary>
        /// <param name = "degree">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float DegreesToRadians(float degree)
        {
            return degree * (MathHelper.Pi / 180.0f);
        }

        /// <summary>
        ///   Converts degrees to gradians.
        /// </summary>
        /// <param name = "degree">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float DegreesToGradians(float degree)
        {
            return degree * (10.0f / 9.0f);
        }

        /// <summary>
        ///   Converts radians to revolutions.
        /// </summary>
        /// <param name = "radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToRevolutions(float radian)
        {
            return radian / MathHelper.TwoPi;
        }

        /// <summary>
        ///   Converts radians to degrees.
        /// </summary>
        /// <param name = "radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToDegrees(float radian)
        {
            return radian * (180.0f / MathHelper.Pi);
        }

        /// <summary>
        ///   Converts radians to gradians.
        /// </summary>
        /// <param name = "radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToGradians(float radian)
        {
            return radian * (200.0f / MathHelper.Pi);
        }

        /// <summary>
        ///   Converts gradians to revolutions.
        /// </summary>
        /// <param name = "gradian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float GradiansToRevolutions(float gradian)
        {
            return gradian / 400.0f;
        }

        /// <summary>
        ///   Converts gradians to degrees.
        /// </summary>
        /// <param name = "gradian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float GradiansToDegrees(float gradian)
        {
            return gradian * (9.0f / 10.0f);
        }

        /// <summary>
        ///   Converts gradians to radians.
        /// </summary>
        /// <param name = "gradian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float GradiansToRadians(float gradian)
        {
            return gradian * (MathHelper.Pi / 200.0f);
        }

        /// <summary>
        /// Performs linear interpolation between two values.
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <param name="s">Amount of interpolation</param>
        /// <returns>The interpolated value</returns>
        public static float Lerp(float a, float b, float s)
        {
            return a + (b - a) * s;
        }

        public static void ComputeTangentBasis(Vector3 P0, Vector3 P1, Vector3 P2, Vector2 UV0, Vector2 UV1, Vector2 UV2, ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal)
        {
            Vector3 e0 = P1 - P0;
            Vector3 e1 = P2 - P0;
            normal = Vector3.Cross(e0, e1);

            //using Eric Lengyel's approach with a few modifications
            //from Mathematics for 3D Game Programmming and Computer Graphics
            // want to be able to trasform a vector in Object Space to Tangent Space
            // such that the x-axis cooresponds to the 's' direction and the
            // y-axis corresponds to the 't' direction, and the z-axis corresponds
            // to <0,0,1>, straight up out of the texture map

            //let P = v1 - v0
            Vector3 P = P1 - P0;

            //let Q = v2 - v0
            Vector3 Q = P2 - P0;

            float s1 = UV1.X - UV0.X;
            float t1 = UV1.Y - UV0.Y;
            float s2 = UV2.X - UV0.X;
            float t2 = UV2.Y - UV0.Y;

            //we need to solve the equation
            // P = s1*T + t1*B
            // Q = s2*T + t2*B

            // for T and B
            //this is a linear system with six unknowns and six equatinos, for TxTyTz BxByBz
            //[px,py,pz] = [s1,t1] * [Tx,Ty,Tz]
            // qx,qy,qz     s2,t2     Bx,By,Bz

            //multiplying both sides by the inverse of the s,t matrix gives
            //[Tx,Ty,Tz] = 1/(s1t2-s2t1) *  [t2,-t1] * [px,py,pz]
            // Bx,By,Bz                      -s2,s1	    qx,qy,qz

            //solve this for the unormalized T and B to get from tangent to object space
            float tmp = 0.0f;

            if (Math.Abs(s1 * t2 - s2 * t1) <= 0.0001f)
                tmp = 1.0f;
            else
                tmp = 1.0f / (s1 * t2 - s2 * t1);

            tangent.X = (t2 * P.X - t1 * Q.X);
            tangent.Y = (t2 * P.Y - t1 * Q.Y);
            tangent.Z = (t2 * P.Z - t1 * Q.Z);
            tangent = tangent * tmp;

            binormal.X = (s1 * Q.X - s2 * P.X);
            binormal.Y = (s1 * Q.Y - s2 * P.Y);
            binormal.Z = (s1 * Q.Z - s2 * P.Z);
            binormal = binormal * tmp;

            normal.Normalize();
            tangent.Normalize();
            binormal.Normalize();
        }
    }
}