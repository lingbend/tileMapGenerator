using System.Numerics;

namespace Vector2Extensions;

public static class Vector2Ext
{
    public static int Testing(this Vector2 vec)
    {
        return 0;
    }

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

    public static Vector2 UP = new Vector2(0, 1);
    public static Vector2 DOWN = new Vector2(0, -1);
    public static Vector2 LEFT = new Vector2(-1, 0);
    public static Vector2 RIGHT = new Vector2(1, 0);
    public static Vector2 CENTER = new Vector2(0, 0);
    public static Vector2 UPLEFT = new Vector2(-1, 1);
    public static Vector2 UPRIGHT = new Vector2(1, 1);
    public static Vector2 DOWNLEFT = new Vector2(-1, -1);
    public static Vector2 DOWNRIGHT = new Vector2(1, -1); 
}