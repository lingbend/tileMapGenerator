namespace CellularRoomGrower;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;

public class CellularRoomGrowerSettings
{
    public Func<Graph, IEnumerable<Room>, Room?> Prioritizer{get; set;} = DefaultPrioritizer;
    public Func<IEnumerable<Vector2>, Room, Vector2> DirectionChooser{get; set;} = DefaultDirectionChooser;
    public List<Vector2> ValidDirections{get; set;} = DefaultValidDirections;
    public static List<Vector2> DefaultValidDirections{get;} = [Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX,
     -Vector2.UnitY];

    public Func<Graph, Vertex, Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>>> ShapeChooser{get; set;} = DefaultShapeChooser;
    public Func<Graph, Vertex, IEnumerable<string>>? Tagger{get; set;} = DefaultTagger;

    internal int MapArea{get; set;}
    public int MaxArea{get; set;} = 10_000;

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
        return (length, direction, corners) =>
        {
            LinkedList<Vector2> shape = new();
            shape.AddLast(new Vector2((int)(-length / 2.0 - 1.0))*Vector2.Abs(new Vector2(direction.Y, direction.X)));
            for (int i = 0; i < length; i++)
            {
                shape.AddLast(shape.Last!.Value+ Vector2.Abs(new Vector2(direction.Y, direction.X)));
            }
            shape.RemoveFirst();
            return shape;
        };
    }

    public static Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> CircularShapeChooser(Graph graph, Vertex vertex)
    {
        // rsintheta = y, rcostheta = x
        return (length, direction, corners) =>
        {
            // HashSet<Vector2> shape = new();
            // (float, float) arc = GetQuarterCircleRadianArc(direction);
            // for (float i = arc.Item1+.01f; i < arc.Item2; i += .01f)
            // {
            //     shape.Add(new Vector2((float) (length * Math.Cos(i)), (float)(length * Math.Sin(i))));                
            // }
            
            // // Correcting any distance from zero for relevant axis
            // HashSet<Vector2> temp_shape = new();
            // foreach (Vector2 point in shape)
            // {
            //     if (Vector2.Abs(10.0f * point * new Vector2(direction.Y, direction.X)).Length() <= ((int) (10f*length/2.0f)))
            //     {
            //         temp_shape.Add(point);
            //     }
            // }
            // Vector2 offset = temp_shape.MinBy((v)=>(v*direction).X + (v*direction).Y) * Vector2.Abs(direction);
            // shape.Clear();
            // foreach (Vector2 point in temp_shape)
            // {
            //     shape.Add(point - offset);
            // }
            // float average = (float) Math.Round(shape.Average((v)=>(v.X*direction.Y) + (v.Y*direction.X)));
            // temp_shape.Clear();
            // foreach (Vector2 point in shape)
            // {
            //     temp_shape.Add(point - new Vector2(average * direction.Y, average * direction.X));
            // }
            // // shape = new HashSet<Vector2>(temp_shape);
            // shape.Clear();
            // foreach (Vector2 point in temp_shape)
            // {
            //     shape.Add(new Vector2((int) Math.Round(point.X),(int) Math.Round(point.Y)));
            // }
            // temp_shape.Clear();
            // if (shape.Count > 4)
            // {
            //     Console.Write("");
            // }
            // return shape;
            HashSet<Vector2> shape = new();
            for (float i = 0; i < Math.PI*2.0f; i += .1f)
            {
                shape.Add(new Vector2((float) (length * Math.Cos(i)), (float)(length * Math.Sin(i))));                
            }
            HashSet<Vector2> temp_shape = new(shape);
            shape = new();
            Vector2 offset = temp_shape.MinBy((v)=>(v*direction).X + (v*direction).Y) * Vector2.Abs(direction);
            foreach (Vector2 point in temp_shape)
            {
                shape.Add(point - offset);
            }
            temp_shape = new(shape);
            shape = new();
            foreach (Vector2 point in temp_shape)
            {
                shape.Add(new Vector2((int) Math.Round(point.X),(int) Math.Round(point.Y)));
            }
            temp_shape = new(shape);
            foreach (Vector2 point in temp_shape)
            {
                if (point.X * direction.X < 0 || point.Y * direction.Y < 0)
                {
                    shape.Remove(point);
                }
                else if (direction.X == 0 && Math.Abs(point.X) < length / 2 || direction.Y == 0 && Math.Abs(point.Y) > length / 2)
                {
                    shape.Remove(point);
                }
            }
            if (shape.Count == 0)
            {
                Console.Write(string.Empty);
                // shape.Add(Vector2.Zero);
            }
            
            
            return shape;
        };
    }

    private static (float, float) GetQuarterCircleRadianArc(Vector2 direction)
    {
        float base_angle = (float) (Math.PI / 4.0f);
        // if x != 0 : sign y = sign y2
        // if y != 0 : sign x = sign x2
        // if x or y < 0 : includes the double negative one (3*)
        // if x or y > 0 : includes the double positive one
        (float, float) arc = (base_angle, base_angle);
        if (direction.X + direction.Y < 0)
        {
            arc.Item1 = 5.0f * base_angle;
            if (Math.Round(Math.Abs(direction.X), 1) > .1)
            {
                arc.Item2 = 3.0f * base_angle;
                // arc = (5.0f * arc.Item1, 7.0f * arc.Item2);;
            }
            else if (Math.Round(Math.Abs(direction.Y), 1) > .1)
            {
                arc.Item2 = 7.0f * base_angle;
            }
        }
        else if (direction.X + direction.Y > 0)
        {
            // arc.Item1 = base_angle; (already defined as such)
            if (Math.Round(direction.X, 1) > .1)
            {
                arc.Item2 = -1.0f * base_angle;
            }
            else if (Math.Round(direction.Y, 1) > .1)
            {
                arc.Item2 = 3.0f * base_angle;
            }
        }
        arc = (Math.Min(arc.Item1, arc.Item2), Math.Max(arc.Item1, arc.Item2));
        return arc;

    }
    

    public static IEnumerable<string> DefaultTagger(Graph graph, Vertex vertex)
    {
        return [];
    }
}