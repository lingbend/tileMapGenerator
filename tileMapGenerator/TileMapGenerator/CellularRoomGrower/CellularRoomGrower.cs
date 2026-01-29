namespace CellularRoomGrower;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<RoomAndEdges.RoomVertex<System.Numerics.Vector2>, RoomAndEdges.RoomEdge<System.Numerics.Vector2>>;
using Vertex = RoomAndEdges.RoomVertex<System.Numerics.Vector2>;
using Edge = RoomAndEdges.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using System.Collections.Concurrent;

public class CellularRoomGrower
{
    public CellularRoomGrowerSettings Settings{get; set;} = new CellularRoomGrowerSettings();

    private Vector2 old_relative_size;

    private string grid_lock = string.Empty;

    public (Graph, BinaryGrid, IEnumerable<Room>, IEnumerable<Hall>) GenerateSizedRooms(Graph graph, int map_area)
    {
        if (Settings.ValidDirections.Count == 0)
        {
            Settings.ValidDirections = CellularRoomGrowerSettings.DefaultValidDirections;
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
                return CellularRoomGrowerSettings.DefaultDirectionChooser(vecs, room);
            }
        };
        Settings.MapArea = map_area;
        old_relative_size = GetOldRelativeSize(graph);
        IEnumerable<Room> rooms = BuildRooms(graph);
        IEnumerable<Hall> halls = BuildHalls(graph);
        BinaryGrid grid = BuildGrid(rooms, halls);
        (rooms, grid) = GrowRooms(rooms, graph, grid, Settings.Prioritizer);

        int retry_count = 0;
        while (retry_count < 10 && Settings.MapArea < Settings.MaxArea && (rooms.Count() < graph.VertexCount || CheckForBadRoomArea(rooms)))
        {
            Settings.MapArea = (int) Math.Ceiling(Math.Pow(Settings.MapArea, 1.1d));
            rooms = BuildRooms(graph);
            halls = BuildHalls(graph);
            grid = BuildGrid(rooms, halls);
            (rooms, grid) = GrowRooms(rooms, graph, grid, Settings.Prioritizer);
            retry_count++;
        }
        return (graph, grid, rooms, halls);
    }

    private bool CheckForBadRoomArea(IEnumerable<Room> rooms)
    {
        foreach (Room room in rooms)
        {
            Vector2 max = room.Corners.Values.Aggregate((v1, v2)=>new Vector2(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y)));
            Vector2 min = room.Corners.Values.Aggregate((v1, v2)=>new Vector2(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y)));
            if (max.X - min.X < 2 || max.Y - min.Y < 2)
            {
                return true;
            }
        }
        return false;
    }

    // Thread safe
    private Vector2 GetOldRelativeSize(Graph graph)
    {
        float max_x = graph.Vertices.Select(v=>v.Weight).MaxBy(v=>v.X).X;
        float max_y = graph.Vertices.Select(v=>v.Weight).MaxBy(v=>v.Y).Y;
        return new Vector2(max_x, max_y);
    }


    // Not thread safe (Tagger)
    internal IEnumerable<Room> BuildRooms(Graph graph)
    {
        List<Room> rooms = new(graph.VertexCount);
        foreach (Vertex vertex in graph.Vertices)
        {
            List<string> tags = new();
            if (Settings.Tagger is not null)
            {
                tags = new(Settings.Tagger(graph, vertex));
            }

            var shape = Settings.ShapeChooser(graph, vertex);
            Room new_room = new Room(vertex, shape, Settings.ValidDirections, AdjustLocus(vertex.Weight, graph.VertexCount), tags);
            rooms.Add(new_room);
        }
        return rooms;
    }

    // thread safe
    internal IEnumerable<Hall> BuildHalls(Graph graph)
    {
        List<Hall> halls = new(graph.EdgeCount);
        foreach (Edge edge in graph.Edges)
        {
            halls.Add(new Hall(edge, AdjustLocus((edge.Source.Weight + edge.Target.Weight) * new Vector2(.5f, .5f), graph.VertexCount)));
        }
        return halls;
    }

    // thread safe
    // Essentially stretches the vector2 according to a matrix transformation
    private Vector2 AdjustLocus(Vector2 locus, int room_number)
    {
        double width_modifier = (Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.X + 1.0) / old_relative_size.X;
        double height_modifier = (Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.Y + 1.0) / old_relative_size.Y;

        if (width_modifier <= 1)
        {
            if (width_modifier < -1)
            {
                width_modifier = (int) Math.Abs(width_modifier);
            }
            else if (width_modifier < 0)
            {
                width_modifier = (int) Math.Abs(width_modifier) + 1;
            }
            else
            {
                width_modifier++;
            }
        }

        if (height_modifier <= 1)
        {
            if (height_modifier < -1)
            {
                height_modifier = (int) Math.Abs(height_modifier);
            }
            else if (height_modifier < 0)
            {
                height_modifier = (int) Math.Abs(height_modifier) + 1;
            }
            else
            {
                height_modifier++;
            }
        }

        return new Vector2((float) Math.Ceiling(locus.X * width_modifier) + 1, (float) Math.Ceiling(locus.Y * height_modifier) + 1);
    }

    // thread safe
    internal BinaryGrid BuildGrid(IEnumerable<Room> rooms, IEnumerable<Hall> halls)
    {
        double width = Math.Ceiling(Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.X + 5.0);
        double height = Math.Ceiling(Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.Y + 5.0);
        BinaryGrid grid = new BinaryGrid((uint) height, (uint) width);
        using var preprocess_rooms = new Task(() =>
        {
            Parallel.ForEach(rooms, (room)=>
            {
                grid.QueueFillCell((uint) room.Locus.Y, (uint) room.Locus.X);
            });
        });

        using var preprocess_halls = new Task(() =>
        {
            Parallel.ForEach(halls, hall =>
            {
                grid.QueueFillCell((uint) hall.Locus.Y, (uint) hall.Locus.X);
            });
        });

        using var check_locs = new Task(() =>
        {
            foreach (Room room in rooms)
            {
                if (grid.GetCell((uint) room.Locus.Y, (uint) room.Locus.X) == 1U)
                {
                    throw new Exception("Locus already occupied: Room failed");
                }
            }

            foreach (Hall hall in halls)
            {
                if (grid.GetCell((uint) hall.Locus.Y, (uint) hall.Locus.X) == 1U)
                {
                    throw new Exception("Locus already occupied: Hall failed");
                }
            }
        });

        check_locs.Start();
        preprocess_rooms.Start();
        preprocess_halls.Start();
        preprocess_rooms.Wait();
        preprocess_halls.Wait();
        check_locs.Wait();
        grid.RunQueue();

        return grid;
    }

    // Not thread safe
    internal (IEnumerable<Room>, BinaryGrid) GrowRooms(IEnumerable<Room> rooms, Graph graph, BinaryGrid grid, Func<Graph, IEnumerable<Room>, IEnumerable<Room>> prioritizer)
    {
        IEnumerable<Room> growable_rooms;
        ConcurrentStack<Room> next_rooms;
        ConcurrentDictionary<Vector2, Room> new_rooms = new();
        while (true)
        {
            int no_rooms_found_counter = 0;
            foreach (Vector2 direction in Settings.ValidDirections)
            {
                growable_rooms = GetGrowableRooms(rooms, grid, direction);
                next_rooms =new(prioritizer(graph, growable_rooms));
                if (next_rooms.Count() == 0 || !growable_rooms.Any())
                {
                    no_rooms_found_counter++;
                }
                else
                {
                    ConcurrentBag<BinaryGrid> new_grids = new();
                    ConcurrentBag<BinaryGrid> negative_grids = new();
                    Parallel.ForEach(next_rooms, next_room =>
                    {
                        (var new_grid, var negative_grid, Room new_room) = GrowRoom(grid, next_room, Settings.DirectionChooser);
                        new_rooms.AddOrUpdate(new_room.Locus, (_)=>new_room, (_, _)=>new_room);
                        new_grids.Add(new_grid);
                        negative_grids.Add(negative_grid);
                    });
                    grid.CombineGrids(new_grids);
                    grid.DifferenceGrids(negative_grids);
                }
            }
            if (no_rooms_found_counter >= 4)
            {
                break;
            }
            
        }
        return (new_rooms.Values.OrderBy(r=>r.ID), grid);
    }

    // thread safe
    private IEnumerable<Room> GetGrowableRooms(IEnumerable<Room> rooms, BinaryGrid grid, Vector2 target_direction)
    {
        ConcurrentBag<Room> growable_rooms = new();
        Parallel.ForEach(rooms, room =>
        {
            float x_to_y_ratio = CalculateSideRatio(room);

            foreach (Vector2 direction in new List<Vector2>(){target_direction})
            {
                if (x_to_y_ratio >= CellularRoomGrowerSettings.MaxRatio.X / CellularRoomGrowerSettings.MaxRatio.Y && direction.X != 0)
                {
                    continue;
                }
                else if (x_to_y_ratio <= CellularRoomGrowerSettings.MaxRatio.Y / CellularRoomGrowerSettings.MaxRatio.X && direction.Y != 0)
                {
                    continue;
                }
                if (CheckDirection(grid, direction, room) && !room.Tags.Contains("_grow_error_"))
                {
                    growable_rooms.Add(room);
                    break;  
                }
            }
        });
        return growable_rooms.OrderBy(r=>r.ID);
    }

    //thread safe
    private float CalculateSideRatio(Room room)
    {
        Vector2 max = room.Corners.Values.Aggregate((v1, v2)=>Vector2.Max(v1, v2));
        Vector2 min = room.Corners.Values.Aggregate((v1, v2)=>Vector2.Min(v1, v2));

        return (max.X-min.X+1) / (max.Y - min.Y+1); 
    }

    // thread safe
    internal (BinaryGrid, BinaryGrid, Room) GrowRoom(BinaryGrid grid, Room room, Func<IEnumerable<Vector2>, Room, Vector2> direction_chooser)
    {
        BinaryGrid grid_copy = new BinaryGrid(grid);
        ConcurrentStack<Vector2> open_directions = new();
        foreach (var direction in Settings.ValidDirections)
        // Parallel.ForEach(Settings.ValidDirections, direction =>
        {
            if (CheckDirection(grid_copy, direction, room))
            {
                open_directions.Push(direction);
            }
        };

        
        if (open_directions is null || !open_directions.Any())
        {
            throw new NullReferenceException("Room cannot grow, no directions found");
        }

        Vector2 chosen_direction;
        Vector2 max = room.Corners.Values.Aggregate((v1, v2)=>new Vector2(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y)));
        Vector2 min = room.Corners.Values.Aggregate((v1, v2)=>new Vector2(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y)));
        if (max.X - min.X < 2 && (open_directions.Contains(Vector2.UnitX) || open_directions.Contains(-Vector2.UnitX)))
        {
            List<Vector2> payload = new();
            if (open_directions.Contains(Vector2.UnitX))
            {
                payload.Add(Vector2.UnitX);
            }
            if (open_directions.Contains(-Vector2.UnitX))
            {
                payload.Add(-Vector2.UnitX);
            }
            chosen_direction = direction_chooser(payload, room);
        }
        else if (max.Y - min.Y < 2 && (open_directions.Contains(Vector2.UnitY) || open_directions.Contains(-Vector2.UnitY)))
        {
            List<Vector2> payload = new();
            if (open_directions.Contains(Vector2.UnitY))
            {
                payload.Add(Vector2.UnitY);
            }
            if (open_directions.Contains(-Vector2.UnitY))
            {
                payload.Add(-Vector2.UnitY);
            }
            chosen_direction = direction_chooser(payload, room);
        }
        else
        {
            chosen_direction = direction_chooser(open_directions.OrderBy(v=>v.X + (5*v.Y)), room);
        }

        BinaryGrid negative_grid = new BinaryGrid(grid_copy.RowSize, grid_copy.ColSize);
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

    // thread safe
    private bool CheckDirection(BinaryGrid grid, Vector2 direction, Room room)
    {
        float x_to_y_ratio = CalculateSideRatio(room);
        if (x_to_y_ratio >= CellularRoomGrowerSettings.MaxRatio.X / CellularRoomGrowerSettings.MaxRatio.Y && direction.X != 0)
        {
            return false;
        }
        else if (x_to_y_ratio <= CellularRoomGrowerSettings.MaxRatio.Y / CellularRoomGrowerSettings.MaxRatio.X && direction.Y != 0)
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

    private (BinaryGrid, BinaryGrid, Room) GrowSide(BinaryGrid grid, Room room, Vector2 side)
    {
        HashSet<Vector2> old_sides = new(room.GetSides());
        HashSet<Vector2>  new_sides = new(room.GetTempGrownSides(side).Item1);
        HashSet<Vector2> old_copy = new(old_sides);
        old_sides.ExceptWith(new_sides);
        new_sides.ExceptWith(old_copy);

        BinaryGrid grid_copy = new BinaryGrid(grid);
        BinaryGrid negative_grid = new BinaryGrid(grid.RowSize, grid.RowSize);

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