namespace MapPrimitives
{
    using System.Numerics;
    using System.Collections.Concurrent;
    using System;
    using System.Collections.Generic;
    
        internal class Side
        {
            public Vector2 Direction{get;}
            public Vector2 Center{get; private set;}
            public int Length{get; private set;}
            private Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> Shape {get;}
            public IEnumerable<Vector2> Points{get; private set;} = new List<Vector2>();

            public Side(Vector2 direction, Vector2 center, Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> shaper, ConcurrentDictionary<Vector2, Vector2> corners, int initial_length=1)
            {
                CheckIfSizeIsValid(initial_length);
             
                Direction = direction;
                Center = center;
                Shape = shaper;
                Points = shaper(initial_length, direction, corners);
                Length = initial_length;
            }


            private void CheckIfSizeIsValid(int size)
            {
                if (size <= 0)
                {
                    throw new ArgumentOutOfRangeException("Sides must have length >= 1");
                }
            }

            internal Side ChangeLengthBy(int size_modifier, ConcurrentDictionary<Vector2, Vector2> corners)
            {
                return SetLengthTo(Length + size_modifier, corners);
            }

            internal Side SetLengthTo(int new_size, ConcurrentDictionary<Vector2, Vector2> corners)
            {
                CheckIfSizeIsValid(new_size);
                return new Side(Direction, Center, Shape, corners, new_size);
            }

            internal Side ChangeCenterBy(Vector2 center_modifier, ConcurrentDictionary<Vector2, Vector2> corners)
            {
                return SetCenterTo(Center + center_modifier, corners);
            }

            internal Side SetCenterTo(Vector2 new_center, ConcurrentDictionary<Vector2, Vector2> corners)
            {
                return new Side(Direction, new_center, Shape, corners, Length);
            }
        }
}
