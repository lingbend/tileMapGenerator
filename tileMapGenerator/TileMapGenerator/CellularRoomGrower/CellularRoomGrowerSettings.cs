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

    public Func<Graph, Vertex, Func<int, Vector2, IEnumerable<Vector2>>> ShapeChooser{get; set;} = DefaultShapeChooser;
    public Func<Graph, Vertex, IEnumerable<string>>? Tagger{get; set;} = DefaultTagger;

    internal int MapArea{get; set;}

    public Vector2 SideRatio{get; set;} = Vector2.One;

    public static Random Random{get;set;} = new Random();
    
    public static Room? DefaultPrioritizer(Graph graph, IEnumerable<Room> rooms)
    {
        return Random.GetItems(rooms.ToArray(), 1).Single();
    }

    public static Vector2 DefaultDirectionChooser(IEnumerable<Vector2> directions, Room room)
    {
        return Random.GetItems(directions.ToArray(), 1).Single();
    }

    public static Func<int, Vector2, IEnumerable<Vector2>> DefaultShapeChooser(Graph graph, Vertex vertex)
    {
        return (length, direction) =>
        {
            int target_length = (length % 2 == 0) ? length : length + 1;
            LinkedList<Vector2> shape = new();
            shape.AddLast(Vector2.Zero);
            for (int i = -target_length / 2; i <= target_length / 2; i++)
            {
                shape.AddLast(shape.Last!.Value+ Vector2.Abs(new Vector2(direction.Y, direction.X)));
            }
            shape.RemoveFirst();
            return shape;
        };
    }

    public static IEnumerable<string> DefaultTagger(Graph graph, Vertex vertex)
    {
        return [];
    }
}