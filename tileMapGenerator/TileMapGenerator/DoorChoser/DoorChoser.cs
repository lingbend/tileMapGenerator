using System.Numerics;

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

namespace DoorChoser;

public static class DoorChoser
{
    public static IEnumerable<Vector2> ChooseDoors(IEnumerable<Room> rooms, IEnumerable<Hall> halls, List<string>? excluded_tags = null)
    {
        ConcurrentDictionary<Room, List<Hall>> rooms_to_halls = new();
        if (excluded_tags is null)
        {
            excluded_tags = [];
        }
        foreach (Hall hall in halls)
        {
            Room source_room = rooms.First(r=>r.Locus == hall.SourceLocus);
            if (!rooms_to_halls.TryAdd(source_room, [hall]) && !source_room.Tags.Any(s=>excluded_tags.Contains(s)))
            {
                rooms_to_halls[source_room].Add(hall);
            }

            Room target_room = rooms.First(r=>r.Locus == hall.TargetLocus);
            if (!rooms_to_halls.TryAdd(target_room, [hall])&& !target_room.Tags.Any(s=>excluded_tags.Contains(s)))
            {
                rooms_to_halls[target_room].Add(hall);
            }
        }
        
        HashSet<Vector2> door_locations = new();
        foreach (Room room in rooms)
        {
            var room_points = room.GetInsidePoints().Union(room.GetSides());
            foreach (Hall hall in rooms_to_halls[room])
            {
                HashSet<Vector2> overlap = new(hall.InsidePoints.Intersect(room_points));
                door_locations.Add(overlap.OrderByDescending(v=>Vector2.DistanceSquared(v, room.Locus)).First());
            }
        }

        return door_locations;
    }

}