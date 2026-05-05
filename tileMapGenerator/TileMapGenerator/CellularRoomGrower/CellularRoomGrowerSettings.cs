namespace CellularRoomGrower
{
    using BinaryGrid;
    using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex<System.Numerics.Vector2>, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
    using Vertex = MapPrimitives.RoomVertex<System.Numerics.Vector2>;
    using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
    using System.Numerics;
    using static System.Math;
    using ConcurrentRandom;
    using System.Collections.Concurrent;
    using Vector2Extensions;
    using MapPrimitives;
    using GoRogueWrapper;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class CellularRoomGrowerSettings
    {
        public Func<Graph, IEnumerable<Room>, IEnumerable<Room>> Prioritizer{get; set;} = DefaultPrioritizer;
        public Func<IEnumerable<Vector2>, Room, Vector2> DirectionChooser{get; set;} = DefaultDirectionChooser;
        internal List<Vector2> ValidDirections{get; set;} = DefaultValidDirections;
        public static List<Vector2> DefaultValidDirections{get;} = new List<Vector2>(){
        Vector2Ext.RIGHT, Vector2Ext.UP, Vector2Ext.LEFT, Vector2Ext.DOWN};

        public Func<Graph, Vertex, Vector2, Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>>> ShapeChooser{get; set;} = DefaultShapeChooser;
        public Func<Graph, Vertex, IEnumerable<string>>? Tagger{get; set;} = DefaultTagger;

        internal int MapArea{get; set;}
        public int MaxArea{get; set;} = 5_000;

        public Vector2 SideRatio{get; set;} = Vector2.One;
        public static Vector2 MaxRatio{get; set;} = new Vector2(2.5f, 1);
        public static Random Random{get;set;}
        private static ConcurrentRandom _direction_random;

        private static ConcurrentRandom _prioritizer_random;
        private static ConcurrentRandom _shaper_random;
    
        public static IEnumerable<Room> DefaultPrioritizer(Graph graph, IEnumerable<Room> rooms)
        {
            if (rooms.Count() == 0)
            {
                return new Room[]{};
            }
            if (_prioritizer_random == null)
            {
                 _prioritizer_random = new ConcurrentRandom(Random.Next());
            }
            IEnumerable<Room> small_rooms = rooms.Where(r=>Vector2Ext.SpanRange(r.GetSides()).X < 3 && Vector2Ext.SpanRange(r.GetSides()).Y<3);
            if (small_rooms.Count() > 0)
            {
                int index = _prioritizer_random.Next(small_rooms.OrderBy(r=>r.Locus.X + (5*r.Locus.Y) + r.ID).Select(r=>r.ID + r.Locus.ToString()).Aggregate((r1, r2)=>r1+r2), 0, small_rooms.Count());
                return new Room[]{small_rooms.OrderBy(r=>r.ID).ElementAt(index)};
            }
            int room_index = _prioritizer_random.Next(rooms.OrderBy(r=>r.Locus.X + (5*r.Locus.Y) + r.ID).Select(r=>r.ID + r.Locus.ToString()).Aggregate((r1, r2)=>r1+r2), 0, rooms.Count());
            return new Room[]{rooms.OrderBy(r=>r.ID).ElementAt(room_index)};
        }

        public static Vector2 DefaultDirectionChooser(IEnumerable<Vector2> directions, Room room)
        {
            if (_direction_random == null)
            {
                 _direction_random = new ConcurrentRandom(Random.Next());
            }
            float x_to_y_ratio = CalculateSideRatio(room);
            List<Vector2> directions_copy = new List<Vector2>(directions);
            if (x_to_y_ratio >= MaxRatio.X / MaxRatio.Y)
            {
                directions_copy = new List<Vector2>(directions_copy.Where(v=>v.X == 0));
            }
            else if (x_to_y_ratio <= MaxRatio.Y / MaxRatio.X)
            {
                directions_copy = new List<Vector2>(directions_copy.Where(v=>v.Y==0));
            }
            if (directions_copy.Count == 0)
            {
                directions_copy = new List<Vector2>(directions);
            }
            int index = _direction_random.Next(room.ID + room.Corners.Values.OrderBy(v=>v.X+(5*v.Y)).Select(v=>v.ToString()).Aggregate((s1, s2)=>s1+s2), 0, directions_copy.Count());
            return directions.OrderBy(v=>v.X + (5*v.Y)).ToArray()[index];
        }

        private static float CalculateSideRatio(Room room)
        {
            return CalculateSideRatio(room.Corners.Values); 
        }

        private static float CalculateSideRatio(IEnumerable<Vector2> corners)
        {
            Vector2 range = Vector2Ext.SpanRange(corners) + Vector2.One;
            return range.X / range.Y;
        }

        public static Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> DefaultShapeChooser(Graph graph, Vertex vertex, Vector2 locus)
        {
            return GetRectangleSides;
        }

        private static IEnumerable<Vector2> GetRectangleSides(int length, Vector2 direction, ConcurrentDictionary<Vector2, Vector2> corners)
        {
            List<Vector2> shape = new List<Vector2>();

            Vector2 range = Vector2Ext.SpanRange(corners.Values);

            foreach (Vector2 dir in new Vector2[] { Vector2Ext.RIGHT, Vector2Ext.UP, Vector2Ext.LEFT, Vector2Ext.DOWN })
            {
                int temp_length;
                if (dir.X == 0)
                {
                    temp_length = (int) (range.X + 1.0);
                }
                else
                {
                    temp_length = (int) (range.Y + 1.0);
                }
                shape.AddRange(GetRectangleSide(temp_length, dir, corners));
            }
            return shape;

        }

        public static IEnumerable<Vector2> GetRectangleSide(int length, Vector2 direction, ConcurrentDictionary<Vector2, Vector2> corners)
        {
            List<Vector2> relevant_corners = new List<Vector2>();

            foreach (var key in corners.Keys)
            {
                if (key.X == direction.X && key.X != 0)
                {
                    relevant_corners.Add(corners[key]);
                }
                else if (key.Y == direction.Y && key.Y != 0)
                {
                    relevant_corners.Add(corners[key]);
                }
            }
            LinkedList<Vector2> shape = new LinkedList<Vector2>();
            Vector2 center = relevant_corners.Aggregate((v1, v2) => v1 + v2) / 2.0f;
            shape.AddLast(new Vector2((int)(-length / 2.0 - 1.0)) * Vector2.Abs(new Vector2(direction.Y, direction.X)));
            for (int i = 0; i < length; i++)
            {
                shape.AddLast(shape.Last!.Value + Vector2.Abs(new Vector2(direction.Y, direction.X)));
            }
            shape.RemoveFirst();
            LinkedList<Vector2> temp_shape = new LinkedList<Vector2>(shape);
            shape = new LinkedList<Vector2>();
            foreach (Vector2 point in temp_shape)
            {
                shape.AddLast((point+center).Ceil());
            }
            return shape;
        }

        public static Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> CircularShapeChooser(Graph graph, Vertex vertex, Vector2 locus)
        {
            return (length, direction, corners)=>{
                var circle_points = GetCircleSides(length, direction, corners);
                return RoundPoints(circle_points);
                };
        }

        private static IEnumerable<Vector2> GetCircleSides(int length, Vector2 direction, ConcurrentDictionary<Vector2, Vector2> corners, float resolution = 1)
        {
            Vector2 center = corners.Values.Aggregate((v1, v2) => v1 + v2) / 4.0f;

            Vector2 min = Vector2Ext.MinRange(corners.Values);
            Vector2 max = Vector2Ext.MaxRange(corners.Values);
            Vector2 diameters = Vector2Ext.SpanRange(corners.Values);
            Vector2 radii = diameters * .5f;

            if (!(diameters.X >= 5 && diameters.Y >= 6 || diameters.X >= 6 && diameters.Y >= 5))
            {
                return GetRectangleSides(length, direction, corners);
            }

            double num_points = (Clamp(Max(Abs(1-CalculateSideRatio(corners.Values)), Abs(1-(1/CalculateSideRatio(corners.Values)))), 1, 5) * 2 * ((2 * diameters.X) + (2 * diameters.Y))) / resolution;

            ConcurrentBag<Vector2> points = new ConcurrentBag<Vector2>();

            Parallel.For(0, (int) (num_points + 1), i=>
            {
                double num = ((i / num_points) * diameters.X) + min.X;
                double offset = num - center.X;
                double parametric_term = radii.Y * Sqrt(1 - (Pow(offset, 2) / Pow(radii.X, 2)));
                points.Add(new Vector2((float)num, center.Y + (float)(parametric_term)));
                points.Add(new Vector2((float)num, center.Y + (float)(-parametric_term)));            
            });

            Parallel.For(0, (int) (num_points + 1), i=>
            {
                double num = ((i / num_points) * diameters.Y) + min.Y;
                double offset = num - center.Y;
                double parametric_term = radii.X * Sqrt(1 - (Pow(offset, 2) / Pow(radii.Y, 2)));
                points.Add(new Vector2(center.X + (float)(parametric_term), (float)num));
                points.Add(new Vector2(center.X + (float)(-parametric_term), (float)num));          
            });

            return points;
        }

        private static IEnumerable<Vector2> RoundPoints(IEnumerable<Vector2> points)
        {
            HashSet<Vector2> temp_points = new HashSet<Vector2>(points);
            ConcurrentDictionary<Vector2, bool> new_points = new ConcurrentDictionary<Vector2, bool>();
            Parallel.ForEach(points, point=>
            {
                new_points.TryAdd(point.Round(), true);
            });
            return new_points.Keys;
        }

        public static Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> CaveShapeChooser(Graph graph, Vertex vertex, Vector2 locus)
        {
            if (_shaper_random is null)
            {
                _shaper_random = new ConcurrentRandom(Random.Next());
            }
            return (length, direction, corners) =>
            {
                Vector2 center = corners.Values.Aggregate((v1, v2)=>v1+v2)/4.0f;
                Vector2 min = Vector2Ext.MinRange(corners.Values);
                Vector2 max = Vector2Ext.MaxRange(corners.Values);
                Vector2 diameters = Vector2Ext.SpanRange(corners.Values);

                float diagonal_length = (corners.Values.First() - center).Length();
                if (!(diameters.X >= 5 && diameters.Y >= 6 || diameters.X >= 6 && diameters.Y >= 5))
                {
                    return RoundPoints(GetCircleSides(length, direction, corners));
                }
                ConcurrentBag<Vector2> points  = new ConcurrentBag<Vector2>(RoundPoints(GetCircleSides(length, direction, corners, 9)));
                ConcurrentRandom rand = new ConcurrentRandom(_shaper_random.Next(center.ToString() + vertex.ID));
                BinaryGrid test_grid = new BinaryGrid((uint) (diameters.Y + 1), (uint) (diameters.X + 1), 0u);
            
                int i_max = 1;
                for (int i = 0; i < i_max + 1; i++)
                {
                    if (i != i_max)
                    {
                        string corner_hashable = corners.Values.OrderBy(v=>v.X+(5*v.Y)).Select(v => v.ToString()).Aggregate((v1, v2)=>v1+v2) + i;
                        Parallel.For(0, (int) (.85 * Pow(diameters.X * diameters.Y, 1.5) / (diameters.X + diameters.Y)), (j) =>
                        {
                            string corner_hashable_copy = corner_hashable;
                            points.Add(rand.NextVector2(corner_hashable_copy+j, (int) min.X, (int) max.X+1, (int) min.Y, (int) max.Y+1));
                        });
                    }
                
                    test_grid.Clear();
                    var neighbors = locus.GetNeighbors();
                    Parallel.ForEach(points.Where(v=>v != locus && !neighbors.Contains(v)), point =>
                    {
                        test_grid.QueueFillCell((uint) (point.Y - min.Y + 1), (uint) (point.X-min.X + 1));
                    });
                    test_grid.RunQueue();
                    points.Clear();
                    for (uint row = 1u; row < (int) diameters.Y + 1; row++)
                    {
                        for (uint col = 1u; col < (int) diameters.X + 1; col++)
                        {                        
                            Vector2 room_coord_vector = new Vector2(col+min.X - 1, row+min.Y - 1);
                            float distance_from_center = (room_coord_vector - center).Length();
                        
                            if (distance_from_center <= .8 * diagonal_length && distance_from_center >= 2)
                            {
                                int num_neighbors = (int) test_grid.GetAllSetCellNeighbors((uint) row, (uint) col);

                                switch (distance_from_center)
                                {
                                    case float f when f < 2.75f:
                                        num_neighbors--;
                                        break;
                                    case float f when f >= .7 * diagonal_length:
                                        num_neighbors++;
                                        break;
                                }
                                if (num_neighbors + i >= 4)
                                {
                                    points.Add(room_coord_vector);
                                }
                            }
                        };
                    };
                }
                var direct_hall = GoRogueWrapper.GetSimpleDirectHall(center, locus);

                points = new ConcurrentBag<Vector2>(points.Union(RoundPoints(GetCircleSides(length, direction, corners))).Except(GoRogueWrapper.GetSimpleDirectHall(center, locus)));

                return points;
            };
        }

        public static IEnumerable<string> DefaultTagger(Graph graph, Vertex vertex)
        {
            return new string[]{};
        }
    }
}