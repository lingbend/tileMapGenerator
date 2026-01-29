namespace MapPrimitives;

using Graph = QuikGraph.UndirectedGraph<RoomVertex<System.Numerics.Vector2>, RoomEdge<System.Numerics.Vector2>>;
using Vertex = RoomVertex<System.Numerics.Vector2>;
using System.Numerics;
using TileMapGenerator;
using System.Collections.Concurrent;
using CellularRoomGrower;

public class Room : IDedThing
{
    public Vector2 Locus {get; private set;}
    public Vertex Vertex {get; private set;}
    public Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> Shape {get; private set;}
    public ConcurrentDictionary<Vector2, Vector2> Corners {get; private set;} = new();
    private List<Side> sides = new();
    public List<string> Tags{get; set;} = new List<string>();
    private int _room_id;
    public int ID { get => _room_id; set => _room_id = value; }

    private ConcurrentDictionary<Vector2, List<Side>> growth_cache = new();
    

    internal Room(Vertex vertex, Func<int, Vector2, ConcurrentDictionary<Vector2, Vector2>, IEnumerable<Vector2>> shaper, IEnumerable<Vector2> valid_directions, Vector2 center, List<string>? tags = null)
    {
        Vertex = vertex;
        Locus = center;
        _room_id = UIDGenerator.GetNextID("room" + vertex.ID + center.X + center.Y);
        Corners.TryAdd(new Vector2(-1, -1), Locus);
        Corners.TryAdd(new Vector2(1, -1), Locus);
        Corners.TryAdd(new Vector2(-1, 1), Locus);
        Corners.TryAdd(new Vector2(1, 1), Locus);
        
        Shape = (num, vec, corners) =>
        {
            var result = shaper(num, vec, (ConcurrentDictionary<Vector2, Vector2>) corners);
            if (result.Count() == 0 || result is null)
            {
                return CellularRoomGrowerSettings.DefaultShapeChooser(new Graph(), vertex)(num, vec, (ConcurrentDictionary<Vector2, Vector2>) corners);
            }
            else
            {
                return result;
            }
        };

        sides.Add(new Side(Vector2.Zero, center, Shape, Corners));
        

        if (tags is not null)
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
        growth_cache.Clear();
    }

    private (IEnumerable<Side>, ConcurrentDictionary<Vector2, Vector2>) CalculateGrowth(Vector2 growing_side, int amount = 1)
    {
        ConcurrentDictionary<Vector2, Vector2> temp_corners = new(Corners);
        

        List<Side> grown_sides_copy = new();

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
        growth_cache.AddOrUpdate(growing_side, (_)=>grown_sides_copy, (_ ,_)=>grown_sides_copy);
        return (grown_sides_copy, temp_corners);
    }
}