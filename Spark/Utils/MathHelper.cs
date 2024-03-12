using System;
using SharpDX;
using Rectangle = SharpDX.Rectangle;
using Point = SharpDX.Point;

namespace Spark
{
    public static partial class MathHelper
    {
        public const float Pi = (float)Math.PI;

        public const float TwoPi = (float)(Math.PI * -2);

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
            (b, a) = (a, b);
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
            return revolution * TwoPi;
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
            return degree * (Pi / 180.0f);
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
            return radian / TwoPi;
        }

        /// <summary>
        ///   Converts radians to degrees.
        /// </summary>
        /// <param name = "radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToDegrees(float radian)
        {
            return radian * (180.0f / Pi);
        }

        /// <summary>
        ///   Converts radians to gradians.
        /// </summary>
        /// <param name = "radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToGradians(float radian)
        {
            return radian * (200.0f / Pi);
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
            return gradian * (Pi / 200.0f);
        }

        [Flags]
        enum OutCode
        {
            Inside = 0,
            Left = 1,
            Right = 2,
            Bottom = 4,
            Top = 8
        }

        private static OutCode ComputeOutCode(float x, float y, Rectangle r)
        {
            var code = OutCode.Inside;

            if (x < r.Left) code |= OutCode.Left;
            if (x > r.Right) code |= OutCode.Right;
            if (y < r.Top) code |= OutCode.Top;
            if (y > r.Bottom) code |= OutCode.Bottom;

            return code;
        }

        private static OutCode ComputeOutCode(Point p, Rectangle r) { return ComputeOutCode(p.X, p.Y, r); }

        private static Point CalculateIntersection(Rectangle r, Point p1, Point p2, OutCode clipTo)
        {
            float dx = (p2.X - p1.X);
            float dy = (p2.Y - p1.Y);

            float slopeY = dx / dy; // slope to use for possibly-vertical lines
            float slopeX = dy / dx; // slope to use for possibly-horizontal lines

            if (clipTo.HasFlag(OutCode.Top))
            {
                return new Point(
                    p1.X + (int)Math.Floor(slopeY * (r.Top - p1.Y)),
                    r.Top
                    );
            }
            if (clipTo.HasFlag(OutCode.Bottom))
            {
                return new Point(
                    p1.X + (int)Math.Floor(slopeY * (r.Bottom - p1.Y)),
                    r.Bottom
                    );
            }
            if (clipTo.HasFlag(OutCode.Right))
            {
                return new Point(
                    r.Right,
                    p1.Y + (int)Math.Floor(slopeX * (r.Right - p1.X))
                    );
            }
            if (clipTo.HasFlag(OutCode.Left))
            {
                return new Point(
                    r.Left,
                    p1.Y + (int)Math.Floor(slopeX * (r.Left - p1.X))
                    );
            }
            throw new ArgumentOutOfRangeException("clipTo = " + clipTo);
        }

        public static bool ClipSegment(ref Point p1, ref Point p2, Rectangle r)
        {
            // classify the endpoints of the line
            var outCodeP1 = ComputeOutCode(p1, r);
            var outCodeP2 = ComputeOutCode(p2, r);
            var accept = false;

            while (true)
            { // should only iterate twice, at most
              // Case 1:
              // both endpoints are within the clipping region
                if ((outCodeP1 | outCodeP2) == OutCode.Inside)
                {
                    accept = true;
                    break;
                }

                // Case 2:
                // both endpoints share an excluded region, impossible for a line between them to be within the clipping region
                if ((outCodeP1 & outCodeP2) != 0)
                {
                    break;
                }

                // Case 3:
                // The endpoints are in different regions, and the segment is partially within the clipping rectangle

                // Select one of the endpoints outside the clipping rectangle
                var outCode = outCodeP1 != OutCode.Inside ? outCodeP1 : outCodeP2;

                // calculate the intersection of the line with the clipping rectangle
                var p = CalculateIntersection(r, p1, p2, outCode);

                // update the point after clipping and recalculate outcode
                if (outCode == outCodeP1)
                {
                    p1 = p;
                    outCodeP1 = ComputeOutCode(p1, r);
                }
                else
                {
                    p2 = p;
                    outCodeP2 = ComputeOutCode(p2, r);
                }
            }

            return accept;
        }
    }
}