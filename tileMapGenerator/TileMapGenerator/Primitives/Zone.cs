namespace Primitives
{
    using Graph = QuikGraph.UndirectedGraph<VectorVertex<System.Numerics.Vector2>, VectorEdge<System.Numerics.Vector2>>;
    using Vertex = VectorVertex<System.Numerics.Vector2>;
    using System.Numerics;
    using TileMapGenerator;
    using System.Collections.Concurrent;
    using CellularGrower;
    using Vector2Extensions;
    using GoRogueWrapper;
    using BitArray2D;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Zone : ID
    {
        public Vector2 Locus {get; private set;}
        public Vertex Vertex {get; private set;}
        public Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> Shape {get; private set;}
        public ConcurrentDictionary<Vector2, Vector2> Corners {get; private set;} = new ConcurrentDictionary<Vector2, Vector2>();
        private List<Side> sides = new List<Side>();
        public List<string> Tags{get; set;} = new List<string>();
        private int _room_id;
        public int ID { get => _room_id; set => _room_id = value; }
        private BitArray2D LocalGrid{get; set;}

        private ConcurrentDictionary<Vector2, List<Side>> growth_cache = new ConcurrentDictionary<Vector2, List<Side>>();
    

        internal Zone(Vertex vertex, Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> shaper, IEnumerable<Vector2> valid_directions, Vector2 center, List<string>? tags = null)
        {
            LocalGrid = new BitArray2D(1, 1);
            Vertex = vertex;
            Locus = center;
            _room_id = UIDGenerator.GetNextID("zone" + vertex.ID + center.X + center.Y);
            Corners.TryAdd(new Vector2(-1, -1), Locus);
            Corners.TryAdd(new Vector2(1, -1), Locus);
            Corners.TryAdd(new Vector2(-1, 1), Locus);
            Corners.TryAdd(new Vector2(1, 1), Locus);
        
            Shape = (num, vec, corners) =>
            {
                var result = shaper(num, vec, (ConcurrentDictionary<Vector2, Vector2>) corners);
                if (result.Count() == 0 || result is null)
                {
                    return CellularGrowerSettings.DefaultShapeChooser(new Graph(), vertex, Locus)(num, vec, (ConcurrentDictionary<Vector2, Vector2>) corners);
                }
                else
                {
                    return result;
                }
            };

            sides.Add(new Side(Vector2.Zero, center, Shape, Corners));
        

            if (tags != null)
            {
                Tags = tags;
            }
        }

        public IEnumerable<Vector2> GetSide(Vector2 direction)
        {
            return sides.First().Points;
        }

        public IEnumerable<Vector2> GetSides()
        {
            return sides.SelectMany((s)=>s.Points);
        }

        public (IEnumerable<Vector2>, ConcurrentDictionary<Vector2, Vector2>) GetTempGrownSides(Vector2 direction, int amount = 1)
        {
            var (new_sides, temp_corners) = CalculateGrowth(direction, amount);
            return (new_sides.SelectMany((side)=>side.Points), temp_corners);
        }

        public void GrowSide(Vector2 direction, int amount = 1)
        {
            var (new_sides, temp_corners) = CalculateGrowth(direction, amount);
            Corners = temp_corners;
            sides = new List<Side>(new_sides);

            Vector2 max = Vec2Ext.MaxRange(Corners.Values);
            Vector2 min = Vec2Ext.MinRange(Corners.Values);
            Vector2 range = Vec2Ext.SpanRange(Corners.Values) + Vector2.One;

            LocalGrid = new BitArray2D((uint) range.Y, (uint) range.X);

            foreach (var side in new_sides)
            {
                foreach (var point in side.Points)
                {
                    LocalGrid.SetCell((point-min+Vector2.One).Reverse(), 1u);
                }
            }
        
            growth_cache.Clear();
        }

        private (IEnumerable<Side>, ConcurrentDictionary<Vector2, Vector2>) CalculateGrowth(Vector2 growing_side, int amount = 1)
        {
            ConcurrentDictionary<Vector2, Vector2> temp_corners = new ConcurrentDictionary<Vector2, Vector2>(Corners);
        

            List<Side> grown_sides_copy = new List<Side>();

            foreach (Vector2 key in Corners.Keys)
            {
                if (key.X == growing_side.X && key.X != 0)
                {
                    temp_corners.TryUpdate(key, temp_corners[key] + new Vector2(growing_side.X, 0), temp_corners[key]);
                }
                else if (key.Y == growing_side.Y && key.Y != 0)
                {
                    temp_corners.TryUpdate(key, temp_corners[key] + new Vector2(0, growing_side.Y), temp_corners[key]);
                }
            }

            if (growth_cache.ContainsKey(growing_side))
            {
                growth_cache.TryGetValue(growing_side, out List<Side>? value);
                return (value, temp_corners)!;
            }

            foreach (Side side in sides)
            {
                var temp_side = side.ChangeCenterBy(growing_side, temp_corners);
                temp_side = temp_side.ChangeLengthBy(amount, temp_corners);
                grown_sides_copy.Add(temp_side);
            }   
            growth_cache.AddOrUpdate(growing_side, (_)=>grown_sides_copy, (a , _)=>grown_sides_copy);
            return (grown_sides_copy, temp_corners);
        }

        public IEnumerable<Vector2> GetInsidePoints()
        {
            ConcurrentDictionary<Vector2, bool> inside = new ConcurrentDictionary<Vector2, bool>();
            inside.TryAdd(Locus, true);
            Vector2 min_corner = Vec2Ext.MinRange(Corners.Values);
            Vector2 max_corner = Vec2Ext.MaxRange(Corners.Values);
            Parallel.ForEach(Vec2Ext.Enumerate(min_corner, max_corner+Vector2.One), point =>
            {
                bool broken = false;
                foreach (var neighbor in point.GetCartesianNeighbors())
                {
                    if (inside.ContainsKey(neighbor) && LocalGrid.GetCell((neighbor-min_corner+Vector2.One).Reverse()) == 0)
                    {
                        inside.TryAdd(point, true);
                        broken = true;
                        break;
                    }
                }
                if (!broken && GoRogue.IsConnected(Locus-min_corner+Vector2.One, point-min_corner+Vector2.One, LocalGrid, Locus))
                {
                    inside.TryAdd(point, true);
                }
            });
            return inside.Keys;
        }
    }

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