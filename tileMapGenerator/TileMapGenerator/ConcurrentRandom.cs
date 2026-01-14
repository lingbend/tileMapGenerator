namespace ConcurrentRandom;

using System.Text;
using System.IO.Hashing;
using System.Numerics;

public class ConcurrentRandom
{

    private int _seed = 0;

    public ConcurrentRandom(int seed)
    {
        _seed = seed;
    }

    public int Next(object unique_state, int min = 0, int max = sizeof(int))
    {
        long xor = XxHash32.HashToUInt32(Encoding.ASCII.GetBytes(unique_state.ToString()!)) ^ _seed;
        int xor_mod = Int32.Abs((int) (xor % (max - min))) + min;
        return xor_mod;
    }

    public Vector2 NextVector2(object unique_state, int min = 0, int max = sizeof(int))
    {
        int x = Next(unique_state.ToString() + 1, min, max);
        int y = Next(unique_state.ToString() + 2, min, max);
        return new Vector2(x, y);
    }

    public int RollMultiple(object unique_state, int sides, int number)
    {
        int result = 0;
        for (int i = 0; i < number; i++)
        {
            result += Next(unique_state, 1, sides + 1);
        }
        int modified_roll = Modify(result, number, number*sides);
        return modified_roll;
    }

    public Vector2 RollMultipleVector2(object unique_state, int sides, int number)
    {
        int x = RollMultiple(unique_state.ToString() + 1, sides, number);
        int y = RollMultiple(unique_state.ToString() + 2, sides, number);
        return new Vector2(x, y);
    }

    public int BellCurved(object unique_state, int min_inclusive, int max_inclusive, uint curve_degree)
    {
        int range = max_inclusive - min_inclusive;
        int sides = (int) (range / curve_degree);
        int result = RollMultiple(unique_state, sides, (int) curve_degree);
        result -= (int) curve_degree;
        result += min_inclusive;
        result = (int) (result * (range / (sides * curve_degree)));
        int modified_roll = Modify(result, min_inclusive, max_inclusive);
        return modified_roll;
    }

    public Vector2 BellCurvedVector2(object unique_state, int min_inclusive, int max_inclusive, uint curve_degree)
    {
        int x = BellCurved(unique_state.ToString() + 1, min_inclusive, max_inclusive, curve_degree);
        int y = BellCurved(unique_state.ToString() + 2, min_inclusive, max_inclusive, curve_degree);
        return new Vector2(x, y);
    }

    internal int Modify(int original, int original_min, int original_max)
    {
        return original;
    }
    
}


