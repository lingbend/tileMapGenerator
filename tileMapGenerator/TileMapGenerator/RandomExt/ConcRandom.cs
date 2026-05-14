namespace ConcRandom
{
    using System.Text;
    using System.IO.Hashing;
    using System.Numerics;
    using System;
    using System.Linq;
    using static Medallion.Bits;

    public class ConcRandom
    {
        private int _seed_1 = 0;
        private int _seed_3 = 0;
        private int _seed_2 = 0;
        private const int MAX_INT = 0b1111111111111111111111111111111;
        private const int MIN = 0;

        public object global_state;

        int[] backing;

        public ConcRandom(int seed)
        {
            if (seed == 0)
            {
                seed += 1;
            }
            _seed_1 = seed;
            _seed_2 = new Random(seed).Next(1, MAX_INT);
            _seed_3 = new Random(_seed_2).Next(1, MAX_INT);
            global_state = _seed_1 + _seed_2 + _seed_3;
            backing = new int[4]{_seed_1, _seed_2, _seed_3, global_state.GetHashCode()};
        } 

        public int Next(object unique_state, int min = MIN, int max = MAX_INT)
        {
            int[] backingCopy = (backing.Select(u=>(int) XxHash32.HashToUInt32(Encoding.ASCII.GetBytes(unique_state.ToString()! + u)))).ToArray();
            int t = backingCopy[1] << 9;
            backingCopy[2] ^= backingCopy[0];
            backingCopy[3] ^= backingCopy[1];
        
            backingCopy[1] ^= backingCopy[2];
            backingCopy[0] ^= backingCopy[3];
            backingCopy[2] ^= t;
            backingCopy[3] = (int) RotateLeft((uint) backingCopy[3], 11);
        
            int result = (int) RotateLeft((uint) backingCopy[0] + (uint) backingCopy[3], 7) + backingCopy[0];
            result = Math.Abs((int) (((long) result+1) % (max - min))) + min;
            return result;
        }

        public bool NextBool(object unique_state)
        {
            return Next(unique_state) % 2 == 1;
        }

        public Vector2 NextVector2(object unique_state, int min_x = 0, int max_x = MAX_INT, int min_y = 0, int max_y = MAX_INT)
        {
            int x = Next(unique_state.ToString() + "homo 13 neanderthalis", min_x, max_x);
            int y = Next(unique_state.ToString() + "teq 13 uila", min_y, max_y);
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
            int x = RollMultiple(unique_state.ToString() + "homo 13 neanderthalis", sides_x, number_x);
            int y = RollMultiple(unique_state.ToString() + "teq 13 uila", sides_y, number_y);
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
            int x = BellCurved(unique_state.ToString() + "homo 13 neanderthalis", min_inclusive_x, max_inclusive_x, curve_degree);
            int y = BellCurved(unique_state.ToString() + "teq 13 uila", min_inclusive_y, max_inclusive_y, curve_degree);
            return new Vector2(x, y);
        }

        internal int Modify(int original, int original_min, int original_max)
        {
            return original;
        }

        public bool CanReset => throw new NotImplementedException();

        public uint Seed => throw new NotImplementedException();

        public int Next()
        {
            throw new NotImplementedException();
        }

        public int Next(int maxValue)
        {
            throw new NotImplementedException();
        }

        public int Next(int minValue, int maxValue)
        {
            throw new NotImplementedException();
        }

        public bool NextBoolean()
        {
            return Next(global_state) % 2 == 1;
        }

        public void NextBytes(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public double NextDouble()
        {
            throw new NotImplementedException();
        }

        public double NextDouble(double maxValue)
        {
            throw new NotImplementedException();
        }

        public double NextDouble(double minValue, double maxValue)
        {
            throw new NotImplementedException();
        }

        public int NextInclusiveMaxValue()
        {
            throw new NotImplementedException();
        }

        public uint NextUInt()
        {
            throw new NotImplementedException();
        }

        public uint NextUInt(uint maxValue)
        {
            throw new NotImplementedException();
        }

        public uint NextUInt(uint minValue, uint maxValue)
        {
            throw new NotImplementedException();
        }

        public uint NextUIntExclusiveMaxValue()
        {
            throw new NotImplementedException();
        }

        public uint NextUIntInclusiveMaxValue()
        {
            throw new NotImplementedException();
        }

        public bool Reset()
        {
            throw new NotImplementedException();
        }

        public bool Reset(uint seed)
        {
            throw new NotImplementedException();
        }
    }


}
