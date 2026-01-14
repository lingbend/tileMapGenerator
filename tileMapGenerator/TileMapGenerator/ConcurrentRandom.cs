namespace ConcurrentRandom;

using System.Text;
using System.IO.Hashing;
using System.Numerics;
using System.ComponentModel;
using Microsoft.VisualBasic;

public class ConcurrentRandom
{

    private int _seed_1 = 0;
    private int _seed_3 = 0;
    private int _seed_2 = 0;
    private const int MAX_INT = 0b1111111111111111111111111111111;

    public ConcurrentRandom(int seed)
    {
        _seed_1 = seed;
        _seed_2 = new Random(seed).Next();
        _seed_3 = new Random(_seed_2).Next();
    }

    public int Next(object unique_state, int min = 0, int max = MAX_INT)
    {
        ulong xor = XxHash32.HashToUInt32(Encoding.ASCII.GetBytes(unique_state.ToString()! + _seed_1));
        // Console.WriteLine(xor + " init ");
        xor *= (ulong) Math.Abs(_seed_3);
        // Console.WriteLine(xor + " times seed 3 ");
        xor ^= (ulong) Math.Pow(264, 59);
        // Console.WriteLine(xor + " xor pow ");
        xor += (ulong) Math.Abs(_seed_2);
        // Console.WriteLine(xor + " add seed 2 ");
        xor ^= 2147483647 >> 11;
        // Console.WriteLine(xor + " shift 11 prime ");
        xor += 1;
        // Console.WriteLine(xor + " +1 ");
        xor ^= xor << 7;
        // Console.WriteLine(xor + " shift 7 ");
        
        xor = (ulong) (BigInteger.Multiply(xor, new BigInteger(Math.Pow(2, 64) - 59)) >> 64);
        // Console.WriteLine(xor + " times pow ");
        xor ^= xor >> 9;
        // Console.WriteLine(xor + " 9 ");
        xor += (uint) ((_seed_3 >> 1) +(_seed_1<<1) + 1);
        xor ^= xor << 9;
        xor += 1;

        // Console.WriteLine(xor);
        

        int xor_mod = Int32.Abs((int) (((long) xor+1) % (max - min))) + min;
        // Console.Write($"min: {min}, max: {max}, mod: ");
        // Console.WriteLine(xor_mod);
        
        return xor_mod;
    }

    public Vector2 NextVector2(object unique_state, int min_x = 0, int max_x = MAX_INT, int min_y = 0, int max_y = MAX_INT)
    {
        int x = Next(unique_state.ToString() + 1, min_x, max_x);
        int y = Next(unique_state.ToString() + 2, min_y, max_y);
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

    public Vector2 RollMultipleVector2(object unique_state, int sides_x, int number_x, int sides_y, int number_y)
    {
        int x = RollMultiple(unique_state.ToString() + 1, sides_x, number_x);
        int y = RollMultiple(unique_state.ToString() + 2, sides_y, number_y);
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

    public Vector2 BellCurvedVector2(object unique_state, int min_inclusive_x, int max_inclusive_x, int min_inclusive_y, int max_inclusive_y, uint curve_degree)
    {
        int x = BellCurved(unique_state.ToString() + 1, min_inclusive_x, max_inclusive_x, curve_degree);
        int y = BellCurved(unique_state.ToString() + 2, min_inclusive_y, max_inclusive_y, curve_degree);
        return new Vector2(x, y);
    }

    internal int Modify(int original, int original_min, int original_max)
    {
        return original;
    }
    
}


