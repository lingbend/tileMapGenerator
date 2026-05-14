namespace CellularGrower
{
    using Grid;
    using Graph = QuikGraph.UndirectedGraph<Primitives.ZVertex<System.Numerics.Vector2>, Primitives.ZEdge<System.Numerics.Vector2>>;
    using Vertex = Primitives.ZVertex<System.Numerics.Vector2>;
    using Edge = Primitives.ZEdge<System.Numerics.Vector2>;
    using System.Numerics;
    using System.Collections.Concurrent;
    using Vector2Extensions;
    using Primitives;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // Step 1?
    public class CellularGrower
    {
        public CellularGrowerSettings Settings{get; set;} = new CellularGrowerSettings();

        private Vector2 old_relative_size;

        private string grid_lock = string.Empty;

        public (Graph, Grid, IEnumerable<Zone>, IEnumerable<Hall>) GenerateZones(Graph graph, int map_area)
        {
            if (Settings.ValidDirections.Count == 0)
            {
                Settings.ValidDirections = CellularGrowerSettings.DefaultValidDirections;
            }
            if (Settings.SideRatio.LengthSquared() == 0)
            {
                Settings.SideRatio = Vector2.One;
            }
            var temp_func = Settings.DirectionChooser;
            Settings.DirectionChooser = (vecs, room) =>
            {
                var result = temp_func(vecs, room);
                if (Settings.ValidDirections.Contains(result))
                {
                    return result;
                }
                else
                {
                    return CellularGrowerSettings.DefaultDirectionChooser(vecs, room);
                }
            };
            Settings.MapArea = map_area;
            old_relative_size = GetOldRelativeSize(graph);
            IEnumerable<Zone> zones = MakeZones(graph);
            IEnumerable<Hall> halls = BuildHalls(graph);
            Grid grid = BuildGrid(zones, halls);
            (zones, grid) = GrowZones(zones, graph, grid, Settings.Prioritizer);

            int retry_count = 0;
            while (retry_count < 10 && Settings.MapArea < Settings.MaxArea && (zones.Count() < graph.VertexCount || CheckForBadArea(zones)))
            {
                Settings.MapArea = (int) Math.Ceiling(Math.Pow(Settings.MapArea, 1.1d));
                zones = MakeZones(graph);
                halls = BuildHalls(graph);
                grid = BuildGrid(zones, halls);
                (zones, grid) = GrowZones(zones, graph, grid, Settings.Prioritizer);
                retry_count++;
            }
            return (graph, grid, zones, halls);
        }

        private bool CheckForBadArea(IEnumerable<Zone> areas)
        {
            foreach (Zone room in areas)
            {
                Vector2 range = Vector2Ext.SpanRange(room.Corners.Values);
                if (range.X < 2 || range.Y < 2)
                {
                    return true;
                }
            }
            return false;
        }

        private Vector2 GetOldRelativeSize(Graph graph)
        {
            Vector2 max = Vector2Ext.MaxRange(graph?.Vertices.Select(v=>(Vector2) v.Weight!)!);
            return max;
        }


        internal IEnumerable<Zone> MakeZones(Graph graph)
        {
            List<Zone> zones = new List<Zone>(graph.VertexCount);
            foreach (Vertex vertex in graph.Vertices)
            {
                List<string> tags = new List<string>();
                if (Settings.Tagger != null)
                {
                    tags = new List<string>(Settings.Tagger(graph, vertex));
                }

                Vector2 locus = AdjustLocus((Vector2) vertex.Weight!, graph.VertexCount);
                var shape = Settings.ShapeChooser(graph, vertex, locus);
                Zone new_room = new Zone(vertex, shape, Settings.ValidDirections, locus, tags);
                zones.Add(new_room);
                vertex["room"] = new_room;
            }
            return zones;
        }

        internal IEnumerable<Hall> BuildHalls(Graph graph)
        {
            List<Hall> halls = new List<Hall>(graph.EdgeCount);
            foreach (Edge edge in graph.Edges)
            {
                var new_hall = new Hall(edge, AdjustLocus(((Vector2) edge.Source.Weight! + (Vector2) edge.Target.Weight!) * new Vector2(.5f, .5f), graph.VertexCount),
                ((Zone)edge.Source["room"]!).Locus,
                ((Zone)edge.Target["room"]!).Locus);
                halls.Add(new_hall);
            }
            return halls;
        }

        private Vector2 AdjustLocus(Vector2 locus, int room_number)
        {
            double width_modifier = (Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.X + 1.0) / old_relative_size.X;
            double height_modifier = (Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.Y + 1.0) / old_relative_size.Y;

            return new Vector2((float) Math.Ceiling(locus.X * width_modifier) + 1, (float) Math.Ceiling(locus.Y * height_modifier) + 1);
        }

        internal Grid BuildGrid(IEnumerable<Zone> zones, IEnumerable<Hall> halls)
        {
            double width = Math.Ceiling(Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.X + 5.0);
            double height = Math.Ceiling(Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.Y + 5.0);
            Grid grid = new Grid((uint)height, (uint)width);
            using var preprocess_zones = new Task(() =>
            {
                Parallel.ForEach(zones, (room) =>
                {
                    grid.QueueFillCell((uint)room.Locus.Y, (uint)room.Locus.X);
                });
            });

            using var preprocess_halls = new Task(() =>
            {
                Parallel.ForEach(halls, hall =>
                {
                    grid.QueueFillCell((uint)hall.Locus.Y, (uint)hall.Locus.X);
                });
            });

            using var check_locs = new Task(()=>CheckAreasOccupied(zones, halls, ref grid));

            check_locs.Start();
            preprocess_zones.Start();
            preprocess_halls.Start();
            preprocess_zones.Wait();
            preprocess_halls.Wait();
            check_locs.Wait();
            grid.RunQueue();

            return grid;
        }

        private void CheckAreasOccupied(IEnumerable<Zone> zones, IEnumerable<Hall> halls, ref Grid grid)
        {
                foreach (Zone room in zones)
                {
                    if (grid.GetCell((uint)room.Locus.Y, (uint)room.Locus.X) == 1U)
                    {
                        throw new Exception("Locus already occupied: Zone failed");
                    }
                }

                foreach (Hall hall in halls)
                {
                    if (grid.GetCell((uint)hall.Locus.Y, (uint)hall.Locus.X) == 1U)
                    {
                        throw new Exception("Locus already occupied: Hall failed");
                    }
                }
        }

        internal (IEnumerable<Zone>, Grid) GrowZones(IEnumerable<Zone> zones, Graph graph, Grid grid, Func<Graph, IEnumerable<Zone>, IEnumerable<Zone>> prioritizer)
        {
            IEnumerable<Zone> growable_zones;
            ConcurrentStack<Zone> next_zones;
            ConcurrentDictionary<Vector2, Zone> new_zones = new ConcurrentDictionary<Vector2, Zone>();
            while (true)
            {
                int no_zones_found_counter = 0;
                foreach (Vector2 direction in Settings.ValidDirections)
                {
                    growable_zones = GetGrowable(zones, grid, direction);
                    next_zones = new ConcurrentStack<Zone>(prioritizer(graph, growable_zones));
                    if (next_zones.Count() == 0 || !growable_zones.Any())
                    {
                        no_zones_found_counter++;
                    }
                    else
                    {
                        ConcurrentBag<Grid> new_grids = new ConcurrentBag<Grid>();
                        ConcurrentBag<Grid> negative_grids = new ConcurrentBag<Grid>();
                        Parallel.ForEach(next_zones, next_room =>
                        {
                            (var new_grid, var negative_grid, Zone new_room) = GrowZone(grid, next_room, Settings.DirectionChooser);
                            new_zones.AddOrUpdate(new_room.Locus, (_)=>new_room, (a, _)=>new_room);
                            new_grids.Add(new_grid);
                            negative_grids.Add(negative_grid);
                        });
                        grid.CombineGrids(new_grids);
                        grid.DifferenceGrids(negative_grids);
                    }
                }
                if (no_zones_found_counter >= 4)
                {
                    break;
                }
            
            }
            return (new_zones.Values.OrderBy(r=>r.ID), grid);
        }

        private IEnumerable<Zone> GetGrowable(IEnumerable<Zone> zones, Grid grid, Vector2 target_direction)
        {
            ConcurrentBag<Zone> growable_zones = new ConcurrentBag<Zone>();
            Parallel.ForEach(zones, room =>
            {
                float x_to_y_ratio = CalculateSideRatio(room);

                foreach (Vector2 direction in new List<Vector2>(){target_direction})
                {
                    if (x_to_y_ratio >= CellularGrowerSettings.MaxRatio.X / CellularGrowerSettings.MaxRatio.Y && direction.X != 0)
                    {
                        continue;
                    }
                    else if (x_to_y_ratio <= CellularGrowerSettings.MaxRatio.Y / CellularGrowerSettings.MaxRatio.X && direction.Y != 0)
                    {
                        continue;
                    }
                    if (CheckDirection(grid, direction, room) && !room.Tags.Contains("_grow_error_"))
                    {
                        growable_zones.Add(room);
                        break;  
                    }
                }
            });
            return growable_zones.OrderBy(r=>r.ID);
        }

        private float CalculateSideRatio(Zone room)
        {
            Vector2 range = Vector2Ext.SpanRange(room.Corners.Values) + Vector2.One;
            return range.X / range.Y;
        }

        internal (Grid, Grid, Zone) GrowZone(Grid grid, Zone room, Func<IEnumerable<Vector2>, Zone, Vector2> direction_chooser)
        {
            Grid grid_copy = new Grid(grid);
            ConcurrentStack<Vector2> open_directions = new ConcurrentStack<Vector2>();
            foreach (var direction in Settings.ValidDirections) // TODO: Implement parallelism
            {
                if (CheckDirection(grid_copy, direction, room))
                {
                    open_directions.Push(direction);
                }
            };

        
            if (open_directions is null || !open_directions.Any())
            {
                throw new NullReferenceException("Zone cannot grow, no directions found");
            }

            Vector2 chosen_direction;
            Vector2 range = Vector2Ext.SpanRange(room.Corners.Values);
            if (range.X < 2 && (open_directions.Contains(Vector2Ext.RIGHT) || open_directions.Contains(Vector2Ext.LEFT)))
            {
                List<Vector2> payload = new List<Vector2>();
                if (open_directions.Contains(Vector2Ext.RIGHT))
                {
                    payload.Add(Vector2Ext.RIGHT);
                }
                if (open_directions.Contains(Vector2Ext.LEFT))
                {
                    payload.Add(Vector2Ext.LEFT);
                }
                chosen_direction = direction_chooser(payload, room);
            }
            else if (range.Y < 2 && (open_directions.Contains(Vector2Ext.UP) || open_directions.Contains(Vector2Ext.DOWN)))
            {
                List<Vector2> payload = new List<Vector2>();
                if (open_directions.Contains(Vector2Ext.UP))
                {
                    payload.Add(Vector2Ext.UP);
                }
                if (open_directions.Contains(Vector2Ext.DOWN))
                {
                    payload.Add(Vector2Ext.DOWN);
                }
                chosen_direction = direction_chooser(payload, room);
            }
            else
            {
                chosen_direction = direction_chooser(open_directions.OrderBy(v=>v.X + (5*v.Y)), room);
            }

            Grid negative_grid = new Grid(grid_copy.RowSize, grid_copy.ColSize);
            if (Settings.ValidDirections.Contains(chosen_direction))
            {
                (grid_copy, negative_grid, room) = GrowSide(grid_copy, room, chosen_direction);
            }
            else
            {
                room.Tags.Add("_grow_error_");
            }
            return (grid_copy, negative_grid, room);
        }

        private bool CheckDirection(Grid grid, Vector2 direction, Zone room)
        {
            float x_to_y_ratio = CalculateSideRatio(room);
            if (x_to_y_ratio >= CellularGrowerSettings.MaxRatio.X / CellularGrowerSettings.MaxRatio.Y && direction.X != 0)
            {
                return false;
            }
            else if (x_to_y_ratio <= CellularGrowerSettings.MaxRatio.Y / CellularGrowerSettings.MaxRatio.X && direction.Y != 0)
            {
                return false;
            }

            var (temp_sides, _) = room.GetTempGrownSides(direction);
            foreach (var spot in temp_sides)
            {
                if (spot.X > grid.ColSize || spot.X < 1 || spot.Y > grid.RowSize || spot.Y < 1)
                {
                    return false;
                }
            }

            var old_sides = room.GetSides();
            var different_sides = temp_sides.Except(old_sides);

            int side_check;
            lock (grid_lock)
            {
                side_check = (int) different_sides.Select(vec=>(vec==room.Locus) ? 0 : grid.GetCell((uint)vec.Y, (uint)vec.X)).Sum(i=>i);
            }
            return side_check == 0;
        }

        private (Grid, Grid, Zone) GrowSide(Grid grid, Zone room, Vector2 side)
        {
            HashSet<Vector2> old_sides = new HashSet<Vector2>(room.GetSides());
            HashSet<Vector2>  new_sides = new HashSet<Vector2>(room.GetTempGrownSides(side).Item1);
            HashSet<Vector2> old_copy = new HashSet<Vector2>(old_sides);
            old_sides.ExceptWith(new_sides);
            new_sides.ExceptWith(old_copy);

            Grid grid_copy = new Grid(grid);
            Grid negative_grid = new Grid(grid.RowSize, grid.RowSize);

            Parallel.ForEach(old_sides, point =>
            {
                negative_grid.QueueFillCell((uint) point.Y, (uint) point.X);
            });

            negative_grid.RunQueue();

            Parallel.ForEach(new_sides, point =>
            {
                grid_copy.QueueFillCell((uint) point.Y, (uint) point.X);
            });

            grid_copy.RunQueue();

            room.GrowSide(side);

            return (grid_copy, negative_grid, room);
        }
    }
}