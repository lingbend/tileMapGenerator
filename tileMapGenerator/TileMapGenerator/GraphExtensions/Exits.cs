using System.Numerics;

using Grid = BitArray2D.BitArray2D;
using System.Collections.Concurrent;
using Primitives;
using Vector2Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Exits
{
    // TODO: Refactor to internal?
    public static class ExitStamper
    {
        public static IEnumerable<Vector2> PlaceExits(IEnumerable<Zone> zones, IEnumerable<Tunnel> tunnels, List<string>? excluded_tags = null)
        {
            ConcurrentDictionary<Zone, List<Tunnel>> rooms_to_halls = new ConcurrentDictionary<Zone, List<Tunnel>>();
            if (excluded_tags is null)
            {
                excluded_tags = new List<string>();
            }
            foreach (Tunnel hall in tunnels)
            {
                Zone source_room = zones.First(r=>r.Locus == hall.SourceLocus);
                if (!rooms_to_halls.TryAdd(source_room, new List<Tunnel>{hall}) && !source_room.Tags.Any(s=>excluded_tags.Contains(s)))
                {
                    rooms_to_halls[source_room].Add(hall);
                }

                Zone target_room = zones.First(r=>r.Locus == hall.TargetLocus);
                if (!rooms_to_halls.TryAdd(target_room, new List<Tunnel>{hall})&& !target_room.Tags.Any(s=>excluded_tags.Contains(s)))
                {
                    rooms_to_halls[target_room].Add(hall);
                }
            }
        
            HashSet<Vector2> exit_locations = new HashSet<Vector2>();
            foreach (Zone room in zones)
            {
                var room_points = room.GetInsidePoints().Union(room.GetSides());
                foreach (Tunnel hall in rooms_to_halls[room])
                {
                    HashSet<Vector2> overlap = new HashSet<Vector2>(hall.InsidePoints.Intersect(room_points));
                    exit_locations.Add(overlap.OrderByDescending(v=>Vector2.DistanceSquared(v, room.Locus)).First());
                }
            }

            return exit_locations;
        }

        public static IEnumerable<Vector2> PatchFrames(IEnumerable<Vector2> exits, global::BitArray2D.BitArray2D grid)
        {
            HashSet<Vector2> patches = new HashSet<Vector2>();
            foreach (Vector2 exit in exits)
            {
                if (grid.GetAllSetCartesianNeighbors((uint) exit.Y, (uint) exit.X) <= 1)
                {
                
                    if (grid.InBounds((exit + Vec2Ext.DOWN).Reverse()) && grid.GetCell((exit + Vec2Ext.DOWN).Reverse()) == 1)
                    {
                        patches.Add(exit + Vec2Ext.UP);
                    }
                    else if (grid.InBounds((exit + Vec2Ext.UP).Reverse()) && grid.GetCell((exit + Vec2Ext.UP).Reverse()) == 1)
                    {
                        patches.Add(exit + Vec2Ext.DOWN);
                    }
                    else if (grid.InBounds((exit + Vec2Ext.LEFT).Reverse()) && grid.GetCell((exit + Vec2Ext.LEFT).Reverse()) == 1)
                    {
                        patches.Add(exit + Vec2Ext.RIGHT);
                    }
                    else if (grid.InBounds((exit + Vec2Ext.RIGHT).Reverse()) && grid.GetCell((exit + Vec2Ext.RIGHT).Reverse()) == 1)
                    {
                        patches.Add(exit + Vec2Ext.LEFT);
                    }
                }
            }
            return patches;
        }

    }
}