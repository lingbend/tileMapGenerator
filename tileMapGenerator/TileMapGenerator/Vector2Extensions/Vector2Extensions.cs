using SadRogue.Primitives;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vector2Extensions
{
    public static class Vec2Ext
    {
        public static Vector2 MaxRange(IEnumerable<Vector2> vectors)
        {
            return vectors.Aggregate((v1, v2)=>Vector2.Max(v1, v2));
        }

        public static Vector2 MinRange(IEnumerable<Vector2> vectors)
        {
            return vectors.Aggregate((v1, v2)=>Vector2.Min(v1, v2));
        }

        public static Vector2 SpanRange(IEnumerable<Vector2> vectors)
        {
            return MaxRange(vectors) - MinRange(vectors);
        }

        public static Vector2 Round(this Vector2 vector)
        {
            return new Vector2((float) Math.Round(vector.X), (float) Math.Round(vector.Y));
        }

        public static Vector2 Ceil(this Vector2 vector)
        {
            return new Vector2((float) Math.Ceiling(vector.X), (float) Math.Ceiling(vector.Y));
        }

        public static Vector2 Floor(this Vector2 vector)
        {
            return new Vector2((float) Math.Floor(vector.X), (float) Math.Floor(vector.Y));
        }

        public static Point ToPoint(this Vector2 vector)
        {
            return new Point((int) vector.X, (int) vector.Y);
        }

        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static IEnumerable<Vector2> GetNeighbors(this Vector2 vector)
        {
            return new Vector2[]{
                vector + UP,
                vector + DOWN,
                vector + LEFT,
                vector + RIGHT,
                vector + UPLEFT,
                vector + UPRIGHT,
                vector + DOWNLEFT,
                vector + DOWNRIGHT
            };
        }

        public static IEnumerable<Vector2> GetCartesianNeighbors(this Vector2 vector)
        {
            return new Vector2[]{
                vector + UP,
                vector + DOWN,
                vector + LEFT,
                vector + RIGHT
            };
        }

        public static IEnumerable<Vector2> Enumerate(Vector2 to_exclusive)
        {
            return Enumerate(Vector2.Zero, to_exclusive);
        }

        public static IEnumerable<Vector2> Enumerate(Vector2 from_inclusive, Vector2 to_exclusive, float step = 1.0f)
        {
            HashSet<Vector2> vectors = new HashSet<Vector2>();
            for (float x = from_inclusive.X; x < to_exclusive.X; x += step)
            {
                for (float y = from_inclusive.Y; y < to_exclusive.Y; y += step)
                {
                    vectors.Add(new Vector2(x, y));
                }
            }
            return vectors;
        }

        public static Vector2 Reverse(this Vector2 vector)
        {
            return new Vector2(vector.Y, vector.X);
        }

        public static Vector2 UP = new Vector2(0, 1);
        public static Vector2 DOWN = new Vector2(0, -1);
        public static Vector2 LEFT = new Vector2(-1, 0);
        public static Vector2 RIGHT = new Vector2(1, 0);
        public static Vector2 CENTER = new Vector2(0, 0);
        public static Vector2 UPLEFT = new Vector2(-1, 1);
        public static Vector2 UPRIGHT = new Vector2(1, 1);
        public static Vector2 DOWNLEFT = new Vector2(-1, -1);
        public static Vector2 DOWNRIGHT = new Vector2(1, -1); 
        public static Vector2 NONE = new Vector2(float.NaN, float.NaN);
    }
}

