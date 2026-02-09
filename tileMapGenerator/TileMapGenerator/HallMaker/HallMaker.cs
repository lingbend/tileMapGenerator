namespace HallMaker;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex<System.Numerics.Vector2>, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
using Vertex = MapPrimitives.RoomVertex<System.Numerics.Vector2>;
using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using System.Collections.Concurrent;
using Vector2Extensions;
using MapPrimitives;
using GoRogueWrapper;
using System.Diagnostics;

public class HallMaker
{
    public HallMaker(){}

    public (Graph, BinaryGrid, IEnumerable<Room>, IEnumerable<Hall>) GenerateHalls(Graph graph, BinaryGrid grid, IEnumerable<Room> rooms, IEnumerable<Hall> halls)
    {
        var room_points = GetRoomInnerPoints(rooms);
        foreach (Hall hall in halls)
        {
            grid = AddHallToAll(grid, room_points, hall);
        }

        return (graph, grid, rooms, halls);
    }

    public BinaryGrid GenerateOnlyHalls(in Graph graph, in BinaryGrid grid, in IEnumerable<Room> rooms, in IEnumerable<Hall> halls)
    {
        var out_grid = new BinaryGrid(grid.RowSize, grid.ColSize);
        var room_points = GetRoomInnerPoints(rooms);
        foreach (Hall hall in halls)
        {
            out_grid.CombineGrids([AddHallAlone(grid, room_points, hall)]);

        }
        return out_grid;
    }

    private BinaryGrid AddHallAlone(in BinaryGrid grid, in IEnumerable<Vector2> room_points, in Hall hall)
    {
        BinaryGrid temp_grid = new BinaryGrid(grid.RowSize, grid.ColSize);
        temp_grid = AddHallInner(room_points, hall, temp_grid);

        return temp_grid;
    }

    private BinaryGrid AddHallToAll(BinaryGrid grid, IEnumerable<Vector2> room_points, Hall hall)
    {
        grid = AddHallInner(room_points, hall, grid);

        return grid;
    }

    private BinaryGrid AddHallInner(IEnumerable<Vector2> room_points, Hall hall, BinaryGrid grid)
    {
        var (inner_points, wall_points) = GenerateHall(hall, room_points);
        foreach (var wall_point in wall_points)
        {
            if (InBounds(wall_point, grid))
            {
                grid.SetCell(wall_point.Reverse(), 1u);
            }

        }
        foreach (var inner_point in inner_points)
        {
            grid.SetCell(inner_point.Reverse(), 0u);
        }
        

        return grid;
    }

    private IEnumerable<Vector2> GetRoomInnerPoints(IEnumerable<Room> rooms)
    {
        ConcurrentBag<IEnumerable<Vector2>> points = new();
        Parallel.ForEach(rooms, room =>
        {
            points.Add(room.GetInsidePoints());
        });
        return new HashSet<Vector2>(points.SelectMany(i=>i));
    }

    private (IEnumerable<Vector2>, IEnumerable<Vector2>) GenerateHall(Hall hall, IEnumerable<Vector2> room_inner)
    {
        HashSet<Vector2> inner_points = new(GenerateInsideHall(hall.Locus, hall.SourceLocus));
        inner_points.UnionWith(GenerateInsideHall(hall.Locus, hall.TargetLocus));
        var wall_points = GenerateHallWalls(inner_points);
        return (inner_points, wall_points.Except(room_inner));
    }

    private IEnumerable<Vector2> GenerateInsideHall(Vector2 source, Vector2 target)
    {
        return GoRogueWrapper.GetSimpleDirectHall(source, target);
    }

    private IEnumerable<Vector2> GenerateHallWalls(IEnumerable<Vector2> inner_points)
    {
        ConcurrentDictionary<Vector2, bool> wall_points = new();
        Parallel.ForEach(inner_points, point =>
        {
            foreach (var neighbor in point.GetNeighbors())
            {
                wall_points.TryAdd(neighbor, true);
            }
        });
        return wall_points.Keys;
    }

    private bool InBounds(Vector2 location, BinaryGrid grid)
    {
        return location.X > 0 && location.Y > 0 && location.X <= grid.ColSize && location.Y <= grid.RowSize;
    }
}