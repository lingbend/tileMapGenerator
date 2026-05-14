using System.Numerics;

using Grid = Grid.Grid;
using System.Collections.Concurrent;
using Primitives;
using Vector2Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Exits
{
    public static class ExitStamper
    {
        public static IEnumerable<Vector2> ChooseExits(IEnumerable<Zone> rooms, IEnumerable<Hall> halls, List<string>? excluded_tags = null)
        {
            ConcurrentDictionary<Zone, List<Hall>> rooms_to_halls = new ConcurrentDictionary<Zone, List<Hall>>();
            if (excluded_tags is null)
            {
                excluded_tags = new List<string>();
            }
            foreach (Hall hall in halls)
            {
                Zone source_room = rooms.First(r=>r.Locus == hall.SourceLocus);
                if (!rooms_to_halls.TryAdd(source_room, new List<Hall>{hall}) && !source_room.Tags.Any(s=>excluded_tags.Contains(s)))
                {
                    rooms_to_halls[source_room].Add(hall);
                }

                Zone target_room = rooms.First(r=>r.Locus == hall.TargetLocus);
                if (!rooms_to_halls.TryAdd(target_room, new List<Hall>{hall})&& !target_room.Tags.Any(s=>excluded_tags.Contains(s)))
                {
                    rooms_to_halls[target_room].Add(hall);
                }
            }
        
            HashSet<Vector2> exit_locations = new HashSet<Vector2>();
            foreach (Zone room in rooms)
            {
                var room_points = room.GetInsidePoints().Union(room.GetSides());
                foreach (Hall hall in rooms_to_halls[room])
                {
                    HashSet<Vector2> overlap = new HashSet<Vector2>(hall.InsidePoints.Intersect(room_points));
                    exit_locations.Add(overlap.OrderByDescending(v=>Vector2.DistanceSquared(v, room.Locus)).First());
                }
            }

            return exit_locations;
        }

        public static IEnumerable<Vector2> PatchFrames(IEnumerable<Vector2> exits, global::Grid.Grid grid)
        {
            HashSet<Vector2> patches = new HashSet<Vector2>();
            foreach (Vector2 exit in exits)
            {
                if (grid.GetAllSetCartesianNeighbors((uint) exit.Y, (uint) exit.X) <= 1)
                {
                
                    if (grid.InBounds((exit + Vector2Ext.DOWN).Reverse()) && grid.GetCell((exit + Vector2Ext.DOWN).Reverse()) == 1)
                    {
                        patches.Add(exit + Vector2Ext.UP);
                    }
                    else if (grid.InBounds((exit + Vector2Ext.UP).Reverse()) && grid.GetCell((exit + Vector2Ext.UP).Reverse()) == 1)
                    {
                        patches.Add(exit + Vector2Ext.DOWN);
                    }
                    else if (grid.InBounds((exit + Vector2Ext.LEFT).Reverse()) && grid.GetCell((exit + Vector2Ext.LEFT).Reverse()) == 1)
                    {
                        patches.Add(exit + Vector2Ext.RIGHT);
                    }
                    else if (grid.InBounds((exit + Vector2Ext.RIGHT).Reverse()) && grid.GetCell((exit + Vector2Ext.RIGHT).Reverse()) == 1)
                    {
                        patches.Add(exit + Vector2Ext.LEFT);
                    }
                }
            }
            return patches;
        }

    }
}