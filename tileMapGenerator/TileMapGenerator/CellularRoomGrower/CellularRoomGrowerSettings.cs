namespace CellularRoomGrower;

using BinaryGrid;
using RoomAndEdges;
using Graph = QuikGraph.UndirectedGraph<RoomAndEdges.RoomVertex<System.Numerics.Vector2>, RoomAndEdges.RoomEdge<System.Numerics.Vector2>>;
using Vertex = RoomAndEdges.RoomVertex<System.Numerics.Vector2>;
using Edge = RoomAndEdges.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using static Math;
using ConcurrentRandom;
using System.Collections.Concurrent;
using System.Drawing;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using Vector2Extensions;

public class CellularRoomGrowerSettings
{
    public Func<Graph, IEnumerable<Room>, IEnumerable<Room>> Prioritizer{get; set;} = DefaultPrioritizer;
    public Func<IEnumerable<Vector2>, Room, Vector2> DirectionChooser{get; set;} = DefaultDirectionChooser;
    internal List<Vector2> ValidDirections{get; set;} = DefaultValidDirections;
    public static List<Vector2> DefaultValidDirections{get;} = [Vector2Ext.RIGHT, Vector2Ext.UP, Vector2Ext.LEFT,
     Vector2Ext.DOWN];

    public Func<Graph, Vertex, Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>>> ShapeChooser{get; set;} = DefaultShapeChooser;
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
            return [];
        }
        if (_prioritizer_random == null)
        {
             _prioritizer_random = new ConcurrentRandom(Random.Next());
        }
        IEnumerable<Room> small_rooms = rooms.Where(r=>(r.GetSides().Max(v=>v.X)-r.GetSides().Min(v=>v.X))<3 && (r.GetSides().Max(v=>v.Y)-r.GetSides().Min(v=>v.Y))<3);
        if (small_rooms.Count() > 0)
        {
            int index = _prioritizer_random.Next(small_rooms.OrderBy(r=>r.Locus.X + (5*r.Locus.Y) + r.ID).Select(r=>r.ID + r.Locus.ToString()).Aggregate((r1, r2)=>r1+r2), 0, small_rooms.Count());
            return [small_rooms.OrderBy(r=>r.ID).ElementAt(index)];
        }
        int room_index = _prioritizer_random.Next(rooms.OrderBy(r=>r.Locus.X + (5*r.Locus.Y) + r.ID).Select(r=>r.ID + r.Locus.ToString()).Aggregate((r1, r2)=>r1+r2), 0, rooms.Count());
        return [rooms.OrderBy(r=>r.ID).ElementAt(room_index)];
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
            directions_copy = new(directions_copy.Where(v=>v.X == 0));
        }
        else if (x_to_y_ratio <= MaxRatio.Y / MaxRatio.X)
        {
            directions_copy = new(directions_copy.Where(v=>v.Y==0));
        }
        if (directions_copy.Count == 0)
        {
            directions_copy = new(directions);
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
        Vector2 max = Vector2Ext.MaxRange(corners);
        Vector2 min = Vector2Ext.MinRange(corners);

        return (max.X-min.X+1) / (max.Y - min.Y+1); 
    }

    public static Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> DefaultShapeChooser(Graph graph, Vertex vertex)
    {
        return GetRectangleSides;

    }

    private static IEnumerable<Vector2> GetRectangleSides(int length, Vector2 direction, ConcurrentDictionary<Vector2, Vector2> corners)
    {

        List<Vector2> shape = new();
        Vector2 max = Vector2Ext.MaxRange(corners.Values);
        Vector2 min = Vector2Ext.MinRange(corners.Values);

        foreach (Vector2 dir in new Vector2[] { Vector2Ext.RIGHT, Vector2Ext.UP, Vector2Ext.LEFT, Vector2Ext.DOWN })
        {
            int temp_length;
            if (dir.X == 0)
            {
                temp_length = (int) (max.X - min.X) + 1;
            }
            else
            {
                temp_length = (int) (max.Y - min.Y) + 1;
            }
            shape.AddRange(GetRectangleSide(temp_length, dir, corners));
        }
        return shape;

    }

    public static IEnumerable<Vector2> GetRectangleSide(int length, Vector2 direction, ConcurrentDictionary<Vector2, Vector2> corners)
    {
        List<Vector2> relevant_corners = new();

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
        LinkedList<Vector2> shape = new();
        Vector2 center = relevant_corners.Aggregate((v1, v2) => v1 + v2) / 2.0f;
        shape.AddLast(new Vector2((int)(-length / 2.0 - 1.0)) * Vector2.Abs(new Vector2(direction.Y, direction.X)));
        for (int i = 0; i < length; i++)
        {
            shape.AddLast(shape.Last!.Value + Vector2.Abs(new Vector2(direction.Y, direction.X)));
        }
        shape.RemoveFirst();
        LinkedList<Vector2> temp_shape = new(shape);
        shape = new();
        foreach (Vector2 point in temp_shape)
        {
            shape.AddLast(new Vector2((float)Math.Round((point + center).X, MidpointRounding.ToPositiveInfinity), (float)Math.Round((point + center).Y, MidpointRounding.ToPositiveInfinity)));
        }
        return shape;
    }

    public static Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> CircularShapeChooser(Graph graph, Vertex vertex)
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
        if (!(max.X - min.X >= 5 && max.Y - min.Y >= 6 || max.X - min.X >= 6 && max.Y - min.Y >= 5))
        {
            return GetRectangleSides(length, direction, corners);
        }

        double x_diameter = (max.X - min.X);
        double y_diameter = (max.Y - min.Y);
        double num_points = (Clamp(Max(Abs(1-CalculateSideRatio(corners.Values)), Abs(1-(1/CalculateSideRatio(corners.Values)))), 1, 5) * 2 * ((2 * x_diameter) + (2 * y_diameter))) / resolution;
        double x_radius = .5 * x_diameter;
        double y_radius = .5 * y_diameter;

        ConcurrentBag<Vector2> points = new();

        Parallel.For(0, (int) (num_points + 1), i=>
        {
            double num = ((i / num_points) * x_diameter) + min.X;
            double offset = num - center.X;
            double parametric_term = y_radius * Sqrt(1 - (Pow(offset, 2) / Pow(x_radius, 2)));
            points.Add(new Vector2((float)num, center.Y + (float)(parametric_term)));
            points.Add(new Vector2((float)num, center.Y + (float)(-parametric_term)));            
        });

        Parallel.For(0, (int) (num_points + 1), i=>
        {
            double num = ((i / num_points) * y_diameter) + min.Y;
            double offset = num - center.Y;
            double parametric_term = x_radius * Sqrt(1 - (Pow(offset, 2) / Pow(y_radius, 2)));
            points.Add(new Vector2(center.X + (float)(parametric_term), (float)num));
            points.Add(new Vector2(center.X + (float)(-parametric_term), (float)num));          
        });

        return points;
    }

    private static IEnumerable<Vector2> RoundPoints(IEnumerable<Vector2> points)
    {
        HashSet<Vector2> temp_points = new(points);
        ConcurrentDictionary<Vector2, bool> new_points = new();
        // foreach (var point in temp_points)
        Parallel.ForEach(points, point=>
        {
            new_points.TryAdd(new Vector2((float)Round(point.X), (float)Round(point.Y)), true);
        });
        return new_points.Keys;
    }

    public static Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> CaveShapeChooser(Graph graph, Vertex vertex)
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
            double x_diameter = (max.X - min.X);
            double y_diameter = (max.Y - min.Y);
            float diagonal_length = (corners.Values.First() - center).Length();
            if (!(x_diameter >= 5 && y_diameter >= 6 || x_diameter >= 6 && y_diameter >= 5))
            {
                return RoundPoints(GetCircleSides(length, direction, corners));
            }
            ConcurrentBag<Vector2> points  = new(RoundPoints(GetCircleSides(length, direction, corners, 9)));
            ConcurrentRandom rand = new(_shaper_random.Next(center.ToString() + vertex.ID));
            BinaryGrid test_grid = new BinaryGrid((uint) (y_diameter + 1), (uint) (x_diameter + 1), 0u);
            Vector2[,] test_points = new Vector2[1,1];
            
            int i_max = 1;
            for (int i = 0; i < i_max + 1; i++)
            {
                if (i != i_max)
                {
                    string corner_hashable = corners.Values.OrderBy(v=>v.X+(5*v.Y)).Select(v => v.ToString()).Aggregate((v1, v2)=>v1+v2) + i;
                    Parallel.For(0, (int) (.85 * Pow(x_diameter * y_diameter, 1.5) / (x_diameter + y_diameter)), (j) =>
                    {
                        string corner_hashable_copy = corner_hashable;
                        points.Add(rand.NextVector2(corner_hashable_copy+j, (int) min.X, (int) max.X+1, (int) min.Y, (int) max.Y+1));
                    });
                }
                
                test_grid.Clear();
                Parallel.ForEach(points, point =>
                {
                    test_grid.QueueFillCell((uint) (point.Y - min.Y + 1), (uint) (point.X-min.X + 1));
                });
                test_grid.RunQueue();
                points.Clear();
                test_points = new Vector2[(int) y_diameter + 1, (int) x_diameter + 1];
                // Parallel.For(1u, (int) y_diameter + 1, row=>
                for (uint row = 1u; row < (int) y_diameter + 1; row++)
                {
                    // Parallel.For(1u, (int) x_diameter + 1, (col, state)=>
                    for (uint col = 1u; col < (int) x_diameter + 1; col++)
                    {
                        Vector2 room_coord_vector = new Vector2(col + min.X - 1, row + min.Y - 1);
                        float distance_from_center = (room_coord_vector - center).Length();
                        int num_neighbors = (int)test_grid.GetAllSetCellNeighbors((uint)row, (uint)col);
                    
                        num_neighbors += (int) Clamp(distance_from_center - 3.75, -1, 0);
                        num_neighbors += (int) Clamp(distance_from_center - (.7 * diagonal_length) + 1, 0, 1);
                        room_coord_vector *= Vector2.Clamp(new Vector2((int) ((.8 * diagonal_length) + 1 - distance_from_center)), Vector2.Zero, Vector2.One);
                        room_coord_vector *= Vector2.Clamp(new Vector2((int) (distance_from_center - 1)), Vector2.Zero, Vector2.One);
                        room_coord_vector *= Vector2.Clamp(new Vector2(num_neighbors + i - 3), Vector2.Zero, Vector2.One);
                        test_points[row, col] = room_coord_vector;

                        
                        // Vector2 room_coord_vector = new Vector2(col+min.X - 1, row+min.Y - 1);
                        // float distance_from_center = (room_coord_vector - center).Length();
                        
                        // if (distance_from_center <= .8 * diagonal_length && distance_from_center >= 2)
                        // {
                  
                        //     num_neighbors = (int) test_grid.GetAllSetCellNeighbors((uint) row, (uint) col);

                        //     switch (distance_from_center)
                        //     {
                        //         case < 2.75f:
                        //             num_neighbors--;
                        //             break;
                        //         case float f when f >= .7 * diagonal_length:
                        //             num_neighbors++;
                        //             break;
                        //     }
                        //     if (num_neighbors + i >= 4)
                        //     {
                        //         test_points[row, col] = room_coord_vector;
                        //         points.Add(room_coord_vector);
                        //     }
                        // }
                    };
                };
                points = new(test_points.Cast<Vector2>().Distinct().Where(v=>v!=Vector2.Zero));
            }
            HashSet<Vector2> temp_results = new();
            foreach (var point in test_points)
            {
                temp_results.Add(point);
            }
            temp_results.Remove(Vector2Ext.CENTER);
            // return temp_results;
            return points;
        };
    }

    public void GPUCellularAutomata(int y_diameter, int x_diameter, Vector2 min, Vector2 max, Vector2 center, float diagonal_length, BinaryGrid test_grid, int iteration)
    {
        Context context = Context.CreateDefaultAutoDebug();
        Accelerator acc = context.GetPreferredDevice(false).CreateAccelerator(context);
        Index2D[] index_array = new Index2D[y_diameter*x_diameter];
        Parallel.For(1, y_diameter+1, row =>
        {
            Parallel.For(1, x_diameter+1, col =>
            {
                index_array[((row-1)*x_diameter)+(col - 1)] =  new Index2D(col, row);
            });
        });
        
        MemoryBuffer1D<Index2D, Stride1D.Dense> indexes = acc.Allocate1D(index_array);
        // Action<Index1D, ArrayView2D<Vector2,Stride2D.General>> kernel = acc.LoadAutoGroupedStreamKernel<Index1D, ArrayView2D<Vector2,Stride2D.General>>(GPUCellularAutomataInner);

        Vector2[,] points = new Vector2[y_diameter, x_diameter];
        // GPUCellularAutomataInner(y_diameter, x_diameter, min, center, diagonal_length, test_grid, points, iteration);
    }

    // private static void GPUCellularAutomataInner(Index1D address_ref, Vector2 min, Vector2 center, float diagonal_length, BinaryGrid test_grid, Vector2[,] points, int i)
    // {
    //     // Index2D address = new Index2D().
    //     Vector2 room_coord_vector = new Vector2(address.X + min.X - 1, address.Y + min.Y - 1);
    //     float distance_from_center = (room_coord_vector - center).Length();
    //     int num_neighbors = (int)test_grid.GetAllSetCellNeighbors((uint)address.Y, (uint)address.X);
    
    //     num_neighbors += (int) Clamp(distance_from_center - 3.75, -1, 0);
    //     num_neighbors += (int) Clamp(distance_from_center - (.7 * diagonal_length) + 1, 0, 1);
    //     room_coord_vector *= Vector2.Clamp(new Vector2((int) ((.8 * diagonal_length) + 1 - distance_from_center)), Vector2.Zero, Vector2.One);
    //     room_coord_vector *= Vector2.Clamp(new Vector2((int) (distance_from_center - 1)), Vector2.Zero, Vector2.One);
    //     room_coord_vector *= Vector2.Clamp(new Vector2(num_neighbors + i - 3), Vector2.Zero, Vector2.One);
    //     points[address.Y, address.X] = room_coord_vector;
        
    // }

    public static IEnumerable<string> DefaultTagger(Graph graph, Vertex vertex)
    {
        return [];
    }
}