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

public class HallMaker
{
    public HallMaker(){}

    public (Graph, BinaryGrid, IEnumerable<Room>, IEnumerable<Hall>) GenerateHalls(Graph graph, BinaryGrid grid, IEnumerable<Room> rooms, IEnumerable<Hall> halls)
    {
        var room_points = GetRoomInnerPoints(rooms);
        HashSet<Vertex> processed_vertices = new();
        foreach (Hall hall in halls)
        {
            if (!processed_vertices.Contains(hall.Edge.Source))
            {
                processed_vertices.Add(hall.Edge.Source);
                grid = AddHall(grid, room_points, hall, hall.Edge.Source.Weight);
            }

            if (!processed_vertices.Contains(hall.Edge.Target))
            {
                processed_vertices.Add(hall.Edge.Target);
                grid = AddHall(grid, room_points, hall, hall.Edge.Target.Weight);
            }
        }

        return (graph, grid, rooms, halls);
    }

    private BinaryGrid AddHall(BinaryGrid grid, IEnumerable<Vector2> room_points, Hall hall, Vector2 target_weight)
    {
        var (inner_points, wall_points) = GenerateHall(hall.Locus, target_weight, room_points);
        foreach (var inner_point in inner_points)
        {
            grid.SetCell(inner_point, 0u);
        }
        foreach (var wall_point in wall_points)
        {
            grid.SetCell(wall_point, 1u);
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

    private (IEnumerable<Vector2>, IEnumerable<Vector2>) GenerateHall(Vector2 source, Vector2 target, IEnumerable<Vector2> room_inner)
    {
        var inner_points = GenerateInsideHall(source, target);
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
        return wall_points.Keys.Except(inner_points);
    }
}