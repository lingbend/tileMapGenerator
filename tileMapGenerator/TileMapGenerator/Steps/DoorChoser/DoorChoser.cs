using System.Numerics;

using Grid = BinaryGrid.BinaryGrid;
using System.Collections.Concurrent;
using MapPrimitives;
using Vector2Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace DoorChoser
{
    public static class DoorChoser
    {
        public static IEnumerable<Vector2> ChooseDoors(IEnumerable<Room> rooms, IEnumerable<Hall> halls, List<string>? excluded_tags = null)
        {
            ConcurrentDictionary<Room, List<Hall>> rooms_to_halls = new ConcurrentDictionary<Room, List<Hall>>();
            if (excluded_tags is null)
            {
                excluded_tags = new List<string>();
            }
            foreach (Hall hall in halls)
            {
                Room source_room = rooms.First(r=>r.Locus == hall.SourceLocus);
                if (!rooms_to_halls.TryAdd(source_room, new List<Hall>{hall}) && !source_room.Tags.Any(s=>excluded_tags.Contains(s)))
                {
                    rooms_to_halls[source_room].Add(hall);
                }

                Room target_room = rooms.First(r=>r.Locus == hall.TargetLocus);
                if (!rooms_to_halls.TryAdd(target_room, new List<Hall>{hall})&& !target_room.Tags.Any(s=>excluded_tags.Contains(s)))
                {
                    rooms_to_halls[target_room].Add(hall);
                }
            }
        
            HashSet<Vector2> door_locations = new HashSet<Vector2>();
            foreach (Room room in rooms)
            {
                var room_points = room.GetInsidePoints().Union(room.GetSides());
                foreach (Hall hall in rooms_to_halls[room])
                {
                    HashSet<Vector2> overlap = new HashSet<Vector2>(hall.InsidePoints.Intersect(room_points));
                    door_locations.Add(overlap.OrderByDescending(v=>Vector2.DistanceSquared(v, room.Locus)).First());
                }
            }

            return door_locations;
        }

        public static IEnumerable<Vector2> PatchDoorframes(IEnumerable<Vector2> doors, Grid grid)
        {
            HashSet<Vector2> patches = new HashSet<Vector2>();
            foreach (Vector2 door in doors)
            {
                if (grid.GetAllSetCartesianNeighbors((uint) door.Y, (uint) door.X) <= 1)
                {
                
                    if (grid.InBounds((door + Vector2Ext.DOWN).Reverse()) && grid.GetCell((door + Vector2Ext.DOWN).Reverse()) == 1)
                    {
                        patches.Add(door + Vector2Ext.UP);
                    }
                    else if (grid.InBounds((door + Vector2Ext.UP).Reverse()) && grid.GetCell((door + Vector2Ext.UP).Reverse()) == 1)
                    {
                        patches.Add(door + Vector2Ext.DOWN);
                    }
                    else if (grid.InBounds((door + Vector2Ext.LEFT).Reverse()) && grid.GetCell((door + Vector2Ext.LEFT).Reverse()) == 1)
                    {
                        patches.Add(door + Vector2Ext.RIGHT);
                    }
                    else if (grid.InBounds((door + Vector2Ext.RIGHT).Reverse()) && grid.GetCell((door + Vector2Ext.RIGHT).Reverse()) == 1)
                    {
                        patches.Add(door + Vector2Ext.LEFT);
                    }
                }
            }
            return patches;
        }

    }
}