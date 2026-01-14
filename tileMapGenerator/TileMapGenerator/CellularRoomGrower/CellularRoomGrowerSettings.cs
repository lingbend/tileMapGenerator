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

public class CellularRoomGrowerSettings
{
    public Func<Graph, IEnumerable<Room>, Room?> Prioritizer{get; set;} = DefaultPrioritizer;
    public Func<IEnumerable<Vector2>, Room, Vector2> DirectionChooser{get; set;} = DefaultDirectionChooser;
    internal List<Vector2> ValidDirections{get; set;} = DefaultValidDirections;
    public static List<Vector2> DefaultValidDirections{get;} = [Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX,
     -Vector2.UnitY];

    public Func<Graph, Vertex, Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>>> ShapeChooser{get; set;} = DefaultShapeChooser;
    public Func<Graph, Vertex, IEnumerable<string>>? Tagger{get; set;} = DefaultTagger;

    internal int MapArea{get; set;}
    public int MaxArea{get; set;} = 5_000;

    public Vector2 SideRatio{get; set;} = Vector2.One;
    public static Random Random{get;set;}
    private static ConcurrentRandom _direction_random;
    
    public static Room? DefaultPrioritizer(Graph graph, IEnumerable<Room> rooms)
    {
        if (rooms.Count() == 0)
        {
            return null;
        }
        IEnumerable<Room> small_rooms = rooms.Where(r=>(r.GetSides().Max(v=>v.X)-r.GetSides().Min(v=>v.X))<3 && (r.GetSides().Max(v=>v.Y)-r.GetSides().Min(v=>v.Y))<3);
        if (small_rooms.Count() > 0)
        {
            return Random.GetItems(small_rooms.OrderBy(r=>r.ID).ToArray(), 1).Single();
        }
        return Random.GetItems(rooms.OrderBy(r=>r.ID).ToArray(), 1).Single();
    }

    public static Vector2 DefaultDirectionChooser(IEnumerable<Vector2> directions, Room room)
    {
        if (_direction_random == null)
        {
             _direction_random = new ConcurrentRandom(Random.Next());
        }
        int index = _direction_random.Next(room.ID + directions.Aggregate((v1, v2)=>v1+v2).ToString() + (room.Locus.X % room.Locus.Y), 0, directions.Count());
        return directions.OrderBy(v=>v.X + (5*v.Y)).ToArray()[index];
    }

    public static Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> DefaultShapeChooser(Graph graph, Vertex vertex)
    {
        return GetRectangleSides;

    }

    private static IEnumerable<Vector2> GetRectangleSides(int length, Vector2 direction, Dictionary<Vector2, Vector2> corners)
    {

        List<Vector2> shape = new();
        Vector2 max = corners.Values.Aggregate((v1, v2)=>Vector2.Max(v1, v2));
        Vector2 min = corners.Values.Aggregate((v1, v2)=>Vector2.Min(v1, v2));
        foreach (Vector2 dir in new Vector2[] { Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY })
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

    public static IEnumerable<Vector2> GetRectangleSide(int length, Vector2 direction, Dictionary<Vector2, Vector2> corners)
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

    public static Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> CircularShapeChooser(Graph graph, Vertex vertex)
    {
        
        return (length, direction, corners)=>{
            var circle_points = GetCircleSides(length, direction, corners);
            return RoundPoints(circle_points);
            };
    }

    private static IEnumerable<Vector2> GetCircleSides(int length, Vector2 direction, Dictionary<Vector2, Vector2> corners, int resolution = 1)
    {
        Vector2 center = corners.Values.Aggregate((v1, v2) => v1 + v2) / 4.0f;
        Vector2 min = corners.Values.Aggregate((v1, v2) => Vector2.Min(v1, v2));
        Vector2 max = corners.Values.Aggregate((v1, v2) => Vector2.Max(v1, v2));
        if (!(max.X - min.X >= 5 && max.Y - min.Y >= 6 || max.X - min.X >= 6 && max.Y - min.Y >= 5))
        {
            return GetRectangleSides(length, direction, corners);
        }

        double x_diameter = (max.X - min.X);
        double y_diameter = (max.Y - min.Y);
        double num_points = (2 * ((2 * x_diameter) + (2 * y_diameter))) / resolution;
        double x_radius = .5 * x_diameter;
        double y_radius = .5 * y_diameter;

        HashSet<Vector2> points = new();

        for (double i = min.X; i <= max.X; i += x_diameter / num_points)
        {
            double i_offset = i - center.X;
            double parametric_term = y_radius * Sqrt(1 - (Pow(i_offset, 2) / Pow(x_radius, 2)));


            points.Add(new Vector2((float)i, center.Y + (float)(parametric_term)));
            points.Add(new Vector2((float)i, center.Y + (float)(-parametric_term)));
        }
        for (double j = min.Y; j <= max.Y; j += y_diameter / num_points)
        {
            double j_offset = j - center.Y;
            double parametric_term = x_radius * Sqrt(1 - (Pow(j_offset, 2) / Pow(y_radius, 2)));


            points.Add(new Vector2(center.X + (float)(parametric_term), (float)j));
            points.Add(new Vector2(center.X + (float)(-parametric_term), (float)j));
        }

        return points;
    }

    private static IEnumerable<Vector2> RoundPoints(IEnumerable<Vector2> points)
    {
        HashSet<Vector2> temp_points = new(points);
        HashSet<Vector2> new_points = new();
        foreach (var point in temp_points)
        {
            new_points.Add(new Vector2((float)Round(point.X), (float)Round(point.Y)));
        }
        return new_points;
    }

    public static Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> CaveShapeChooser(Graph graph, Vertex vertex)
    {
        return (length, direction, corners) =>
        {
            Vector2 center = corners.Values.Aggregate((v1, v2)=>v1+v2)/4.0f;
            Vector2 min = corners.Values.Aggregate((v1, v2)=>Vector2.Min(v1, v2));
            Vector2 max = corners.Values.Aggregate((v1, v2)=>Vector2.Max(v1, v2));
            double x_diameter = (max.X - min.X);
            double y_diameter = (max.Y - min.Y);
            float diagonal_length = (corners.Values.First() - center).Length();
            if (!(max.X - min.X >= 7 && max.Y - min.Y >= 8 || max.X - min.X >= 8 && max.Y - min.Y >= 7))
            {
                return GetRectangleSides(length, direction, corners);
            }
            HashSet<Vector2> circle_points = new(RoundPoints(GetCircleSides(length, direction, corners, 6)));
            ConcurrentBag<Vector2> points = new(circle_points);
            
            int i_max = 3;
            for (int i = 0; i < i_max + 1; i++)
            {
                string corner_hashable = corners.Values.OrderBy(v=>v.X+(5*v.Y)).Select(v => v.ToString()).Order().Aggregate((v1, v2)=>v1+v2);
                if (i != i_max)
                {
                    ConcurrentRandom rand = new(Random.Next());
                    Parallel.For(0, (int) (Pow(x_diameter * y_diameter, 1.5) / (((x_diameter + y_diameter) * 2.25) + (i * .5))), (j) =>
                    {
                        string corner_hashable_copy = corner_hashable;
                        points.Add(rand.NextVector2(corner_hashable_copy+j, (int) min.X, (int) max.X + 1, (int) min.Y, (int) max.Y+1));
                    });
                }
                
                BinaryGrid test_grid = new BinaryGrid((uint) (y_diameter + 1), (uint) (x_diameter + 1), 0);
                foreach (Vector2 point in points)
                {
                    test_grid.SetCell((uint) (point.Y - min.Y + 1), (uint) (point.X-min.X + 1), 1u);
                }
                points.Clear();
                for (uint row = 1; row <= y_diameter; row++)
                {
                    for (uint col = 1; col <= x_diameter; col++)
                    {
                        int num_neighbors = (int) test_grid.GetAllSetCellNeighbors(row, col);
                        int center_modifier = 0;
                        Vector2 room_coord_vector = new Vector2(col+min.X - 1, row+min.Y - 1);
                        float distance_from_center = (room_coord_vector - center).Length();

                        if (distance_from_center < 2)
                        {
                            continue;
                        }
                        if (distance_from_center < 3)
                        {
                            center_modifier = -1;
                        }
                        else if (i == 0 && distance_from_center <= diagonal_length * .9 && distance_from_center >= diagonal_length * .80 && (Abs((room_coord_vector - center).X) < .5 || Abs((room_coord_vector - center).Y) < 1))
                        {
                            center_modifier = -1;
                        }

                        num_neighbors += center_modifier;

                        if (circle_points.Contains(room_coord_vector) && num_neighbors >= 3)
                        {
                            points.Add(room_coord_vector);   
                        }
                        else if (num_neighbors >= 4)
                        {
                            points.Add(room_coord_vector);
                        }
                        else if(num_neighbors + i - center_modifier >= 8)
                        {
                            points.Add(room_coord_vector);
                        }
                    }
                }
            }
            return points;
        };
    }

    public static IEnumerable<string> DefaultTagger(Graph graph, Vertex vertex)
    {
        return [];
    }
}