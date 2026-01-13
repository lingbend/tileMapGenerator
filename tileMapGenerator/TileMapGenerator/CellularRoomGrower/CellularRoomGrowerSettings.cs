namespace CellularRoomGrower;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using static Math;
using System.Runtime.Intrinsics;
using System.Linq.Expressions;

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

    public static Random Random{get;set;} = new Random();
    
    public static Room? DefaultPrioritizer(Graph graph, IEnumerable<Room> rooms)
    {
        if (rooms.Count() == 0)
        {
            return null;
        }
        IEnumerable<Room> small_rooms = rooms.Where(r=>(r.GetSides().Max(v=>v.X)-r.GetSides().Min(v=>v.X))<3 && (r.GetSides().Max(v=>v.Y)-r.GetSides().Min(v=>v.Y))<3);
        if (small_rooms.Count() > 0)
        {
            return Random.GetItems(small_rooms.ToArray(), 1).Single();
        }
        return Random.GetItems(rooms.ToArray(), 1).Single();
    }

    public static Vector2 DefaultDirectionChooser(IEnumerable<Vector2> directions, Room room)
    {
        return Random.GetItems(directions.ToArray(), 1).Single();
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

    private static IEnumerable<Vector2> GetCircleSides(int length, Vector2 direction, Dictionary<Vector2, Vector2> corners)
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
        double num_points = 2 * ((2 * x_diameter) + (2 * y_diameter));
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
            float diagonal_length = (corners.Values.First() - center).Length();
            if (!(max.X - min.X >= 7 && max.Y - min.Y >= 8 || max.X - min.X >= 8 && max.Y - min.Y >= 7))
            {
                return GetRectangleSides(length, direction, corners);
            }
            HashSet<Vector2> circle_points = new(GetCircleSides(length, direction, corners));
            circle_points = new(RoundPoints(circle_points));
            HashSet<Vector2> points = new(circle_points);
            
            int i_max = 3;
            for (int i = 0; i < i_max + 1; i++)
            {
                if (i != i_max)
                {
                    for (int j = 0; j < Pow((max-min).X * (max-min).Y, 1.5) / ((((max - min).X + (max-min).Y) * 2.25) + (i * .33)); j++)
                    {
                        points.Add(new Vector2(Random.Next((int) min.X, (int) max.X + 1), Random.Next((int) min.Y, (int) max.Y + 1)));
                    }
                }
                
                BinaryGrid test_grid = new BinaryGrid((uint) (max.Y - min.Y + 1), (uint) (max.X - min.X + 1), 0);
                foreach (Vector2 point in points)
                {
                    test_grid.SetCell((uint) (point.Y - min.Y + 1), (uint) (point.X-min.X + 1), 1u);
                }
                HashSet<Vector2> temp_points = new(points);
                points.Clear();
                for (uint row = 1; row <= (max.Y - min.Y); row++)
                {
                    for (uint col = 1; col <= (max.X - min.X); col++)
                    {
                        int num_neighbors = (int) test_grid.GetAllSetCellNeighbors(row, col);
                        int center_modifier;
                        float distance_from_center = (new Vector2(col, row) - center).Length();
                        if (distance_from_center <= 5)
                        {
                            center_modifier = -9;
                        }
                        else if (distance_from_center <= .15f * diagonal_length)
                        {
                            center_modifier = -2;
                        }
                        else if (distance_from_center <= .4f * diagonal_length)
                        {
                            center_modifier = -1;
                        }
                        else
                        {
                            center_modifier = 0;
                        }
                        if (circle_points.Contains(new Vector2(col+min.X - 1, row+min.Y - 1)) && (num_neighbors + center_modifier) >= 3)
                        {
                            points.Add(new Vector2(col+min.X - 1, row+min.Y - 1));   
                        }
                        else if ((num_neighbors + center_modifier) >= 4)
                        {
                            points.Add(new Vector2(col+min.X - 1, row+min.Y - 1));
                        }
                        else if(num_neighbors + i >= 8 && center_modifier != -9)
                        {
                            points.Add(new Vector2(col+min.X - 1, row+min.Y - 1));
                        }
                    }
                }
            }
            


            // List<Vector2> relevant_corners = new(corners.Values);
            // Vector2 start = relevant_corners[0];
            // Vector2 end = relevant_corners[1];
            // Vector2 false_end = end  + (direction * new Vector2(-5));
            // HashSet<Vector2> points = new(){start};
            // Vector2 current = start;
            // int stagger_counter = 0;
            // Vector2 last_direction = Vector2.Zero;
            // double temp_target;
            // while (current != false_end)
            // {
            //     Vector2 stagger_direction;
            //     List<Vector2> directions = GetDrunkenOptions(current, center, start, end, last_direction);
            //     if (directions.Any())
            //     {
            //         stagger_direction = directions[Random.Next(directions.Count)];
            //     }
            //     else
            //     {
            //         stagger_direction = Vector2.Normalize(false_end - current);
            //     }
                
            //     if (stagger_counter == 1)
            //     {
            //         stagger_direction += Vector2.Normalize((false_end - start) + (direction * new Vector2(Random.Next(-4, 14) + Random.Next(-4, 14))));
            //         if (Abs(stagger_direction.X) > 1)
            //         {
            //             stagger_direction.X /= Abs(stagger_direction.X);
            //         }
            //         if (Abs(stagger_direction.Y) > 1)
            //         {
            //             stagger_direction.Y /= Abs(stagger_direction.Y);
            //         }
            //     }
            //     if (stagger_counter >= 3)
            //     {
            //         stagger_direction += Vector2.Normalize(false_end - current);
            //         if (Abs(stagger_direction.X) > 1)
            //         {
            //             stagger_direction.X /= Abs(stagger_direction.X);
            //         }
            //         if (Abs(stagger_direction.Y) > 1)
            //         {
            //             stagger_direction.Y /= Abs(stagger_direction.Y);
            //         }
            //         stagger_counter = 0;
            //     }
            //     if (stagger_direction == Vector2.Normalize(false_end - start) || stagger_direction == Vector2.Normalize(false_end - current))
            //     {
            //         stagger_counter = 0;
            //     }
                
            //     stagger_direction = new Vector2((float) Round(stagger_direction.X, MidpointRounding.AwayFromZero), (float) Round(stagger_direction.Y, MidpointRounding.AwayFromZero));
            //     current += stagger_direction;
            //     points.Add(current);
            //     last_direction = stagger_direction;
            //     stagger_counter++;
            // }

            // // HashSet<Vector2> temp_points = new(points);
            // // points.Clear();
            // // Vector2 last_point = Vector2.Zero;
            // // foreach (var point in temp_points.OrderBy(v=> (v * direction).Length()))
            // // {
            // //     if (last_point != Vector2.Zero)
            // //     {
            // //         points.Add(Vector2.Lerp(last_point, point, .5f));
            // //     }
            // //     else
            // //     {
            // //         points.Add(point);
            // //     }
            // //     last_point = point;
            // // }
            // // points.Add(last_point);

            // // temp_points = new(points);
            // // points.Clear();
            // // last_point = Vector2.Zero;
            // // foreach (var point in temp_points.OrderBy(v=> (v * direction).Length()))
            // // {
            // //     if (last_point != Vector2.Zero)
            // //     {
            // //         points.Add(Vector2.Lerp(last_point, point, .5f));
            // //     }
            // //     else
            // //     {
            // //         points.Add(point);
            // //     }
            // //     last_point = point;
            // // }
            // // points.Add(last_point);


            // HashSet<Vector2> temp_points = new(points);
            // points.Clear();
            // foreach (var point in temp_points)
            // {
            //     points.Add(new Vector2((float) Round(point.X), (float) Round(point.Y)));
            // }
            // return points;
            return RoundPoints(points);
        };
    }

    private static List<Vector2> GetDrunkenOptions(Vector2 current_point, Vector2 center, Vector2 start, Vector2 end, Vector2 last, float step = 1f)
    {
        List<Vector2> directions = new(){Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY, new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1)};
        Vector2 wrong_direction = Vector2.Normalize(start-end);
        directions.Remove(wrong_direction);

        foreach (Vector2 direction in new List<Vector2>(directions))
        {
            if (!InQuarter(current_point + (direction * step), center, start, end))
            {
                directions.Remove(direction);
            }
            else if (last != Vector2.Zero)
            {
                if (direction == last)
                {
                    directions.AddRange([last, last]);
                }
                else if ((direction - last).LengthSquared() <= 1)
                {
                    directions.Add(direction);
                }
                else if (direction == Vector2.Normalize(end - start))
                {
                    directions.Add(direction);
                }
                else
                {
                    if (start.X == end.X && direction.Y == wrong_direction.Y)
                    {
                        directions.Remove(direction);
                    }
                    else if (start.Y == end.Y && direction.X == wrong_direction.X)
                    {
                        directions.Remove(direction);
                    }
                }
            }
        }
        return directions;
    }


    private static bool InQuarter(Vector2 point, Vector2 center, Vector2 corner1, Vector2 corner2)
    {
        Vector2 max = Vector2.Max(Vector2.Max(corner1, corner2), center);
        Vector2 min = Vector2.Min(Vector2.Min(corner1, corner2), center);

        float end_padding_x = .5f * (max.X - min.X);
        float end_padding_y = .5f * (max.Y-min.Y);

        if (corner1.X == corner2.X && corner1.X == 1)
        {
            max.X += end_padding_x;
        }
        else if (corner1.X == corner2.X && corner1.X == -1)
        {
            min.X -= end_padding_x;
        }
        else if (corner1.Y == corner2.Y && corner1.Y == 1)
        {
            max.Y += end_padding_y;
        }
        else if (corner1.Y == corner2.Y && corner1.Y == -1)
        {
            min.Y -= end_padding_y;
        }
        

        Vector2 average_corner = (corner1 + corner2) / 2f;
        Vector2 vec_to_average = (average_corner - point);
        Vector2 corner_prime_1, corner_prime_2;

        if (corner1.X == corner2.X)
        {
            corner_prime_1 = new Vector2(center.X, corner1.Y);
            corner_prime_2 = new Vector2(center.X, corner2.Y);
        }
        else
        {
            corner_prime_1 = new Vector2(corner1.X, center.Y);
            corner_prime_2 = new Vector2(corner2.X, center.Y);
        }

        Vector2 vec_to_prime_1 = (corner_prime_1 - point);
        Vector2 vec_to_prime_2 = (corner_prime_2 - point);

        if (point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y
        && vec_to_average.LengthSquared() <= vec_to_prime_1.LengthSquared()
        && vec_to_average.LengthSquared() <= vec_to_prime_2.LengthSquared())
        {
            return true;
        }

        return false;
    }



    public static IEnumerable<string> DefaultTagger(Graph graph, Vertex vertex)
    {
        return [];
    }
}