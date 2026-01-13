namespace CellularRoomGrower;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using System.Diagnostics;

public class CellularRoomGrower
{


    public CellularRoomGrowerSettings Settings{get; set;} = new CellularRoomGrowerSettings();

    private Vector2 old_relative_size;

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

    private Vector2 GetOldRelativeSize(Graph graph)
    {
        float max_x = graph.Vertices.Select(v=>v.Weight).MaxBy(v=>v.X).X;
        float max_y = graph.Vertices.Select(v=>v.Weight).MaxBy(v=>v.Y).Y;
        return new Vector2(max_x, max_y);
    }


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
            Room.SideType room_type = Room.SideType.CIRCLE;
            Room new_room = new Room(vertex, shape, Settings.ValidDirections, AdjustLocus(vertex.Weight, graph.VertexCount), room_type, tags);
            rooms.Add(new_room);
        }
        return rooms;
    }

    internal IEnumerable<Hall> BuildHalls(Graph graph)
    {
        List<Hall> halls = new(graph.EdgeCount);
        foreach (Edge edge in graph.Edges)
        {
            halls.Add(new Hall(edge, AdjustLocus((edge.Source.Weight + edge.Target.Weight) * new Vector2(.5f, .5f), graph.VertexCount)));
        }
        return halls;
    }

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

    internal BinaryGrid BuildGrid(IEnumerable<Room> rooms, IEnumerable<Hall> halls)
    {
        double width = Math.Ceiling(Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.X + 5.0);
        double height = Math.Ceiling(Math.Pow(Settings.MapArea, .75) * Settings.SideRatio.Y + 5.0);
        BinaryGrid grid = new BinaryGrid((uint) height, (uint) width);

        foreach (Room room in rooms)
        {
            if (grid.GetCell((uint) room.Locus.Y, (uint) room.Locus.X) == 1U)
            {
                throw new Exception("Locus already occupied: Room failed");
            }
            grid.SetCell((uint) room.Locus.Y, (uint) room.Locus.X, 1U);
        }

        foreach (Hall hall in halls)
        {
            if (grid.GetCell((uint) hall.Locus.Y, (uint) hall.Locus.X) == 1U)
            {
                throw new Exception("Locus already occupied: Hall failed");
            }
            grid.SetCell((uint) hall.Locus.Y, (uint) hall.Locus.X, 1U);
        }

        return grid;
    }

    internal (IEnumerable<Room>, BinaryGrid) GrowRooms(IEnumerable<Room> rooms, Graph graph, BinaryGrid grid, Func<Graph, IEnumerable<Room>, Room?> prioritizer)
    {
        Room? next_room;
        IEnumerable<Room> growable_rooms = rooms;
        HashSet<Room> new_rooms = new(rooms.Count());
        while ( (growable_rooms = GetGrowableRooms(growable_rooms, grid)).Any() && (next_room = prioritizer(graph, growable_rooms)) is not null)
        {
            (grid, Room new_room) = GrowRoom(grid, next_room, Settings.DirectionChooser);
            new_rooms.Add(new_room);
        }
        return (new_rooms, grid);
    }

    private IEnumerable<Room> GetGrowableRooms(IEnumerable<Room> rooms, BinaryGrid grid)
    {
        List<Room> growable_rooms = new((int) (rooms.Count()*.5));
        foreach (Room room in rooms)
        {
            foreach (Vector2 direction in Settings.ValidDirections)
            {
                if (CheckDirection(grid, direction, room) && !room.Tags.Contains("_grow_error_"))
                {
                    growable_rooms.Add(room);
                    break;  
                }
            }
        }
        return growable_rooms;
    }

    internal (BinaryGrid, Room) GrowRoom(BinaryGrid grid, Room room, Func<IEnumerable<Vector2>, Room, Vector2> direction_chooser)
    {
        List<Vector2> open_directions = new();
        foreach (Vector2 direction in Settings.ValidDirections)
        {
            if (CheckDirection(grid, direction, room))
            {
                open_directions.Add(direction);
            }
        }

        
        if (open_directions is null || !open_directions.Any())
        {
            throw new NullReferenceException("Room cannot grow, no directions found");
        }

        Vector2 chosen_direction;

        // Vector2 max = room.GetSides().Aggregate((v1, v2)=>new Vector2(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y)));
        // Vector2 min = room.GetSides().Aggregate((v1, v2)=>new Vector2(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y)));
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
            chosen_direction = direction_chooser(open_directions, room);
        }

        
        if (Settings.ValidDirections.Contains(chosen_direction))
        {
            (grid, room) = GrowSide(grid, room, chosen_direction);
        }
        else
        {
            room.Tags.Add("_grow_error_");
        }
        return (grid, room);
    }

    private bool CheckDirection(BinaryGrid grid, Vector2 direction, Room room)
    {
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
        
        int side_check = (int) different_sides.Select(vec=>(vec==room.Locus) ? 0 : grid.GetCell((uint)vec.Y, (uint)vec.X)).Sum(i=>i);
        return side_check == 0;
    }

    private (BinaryGrid, Room) GrowSide(BinaryGrid grid, Room room, Vector2 side)
    {
        foreach (Vector2 point in room.GetSides())
        {
            grid.SetCell((uint) point.Y, (uint) point.X, 0U);
        }

        foreach (Vector2 point in  room.GetTempGrownSides(side).Item1)
        {
            grid.SetCell((uint) point.Y, (uint) point.X, 1U);
        }

        room.GrowSide(side);

        return (grid, room);
    }
}

public class Room
{
    public Vector2 Locus {get; private set;}
    public Vertex Vertex {get; private set;}
    public Func<int, Vector2, IDictionary<Vector2, Vector2>, IEnumerable<Vector2>> Shape {get; private set;}
    // private List<Vector2> corners = new();
    public Dictionary<Vector2, Vector2> Corners {get; private set;} = new();
    private List<Side> sides = new();
    public List<string> Tags{get; set;} = new List<string>();
    private Dictionary<Vector2, List<Side>> growth_cache = new();

    public enum SideType
    {
        RECT,
        CIRCLE
    }

    public SideType sideType {get; private set;} = SideType.CIRCLE;
    

    internal Room(Vertex vertex, Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> shaper, IEnumerable<Vector2> valid_directions, Vector2 center, SideType side_type=SideType.RECT, List<string>? tags = null)
    {
        sideType = side_type;
        Vertex = vertex;
        Locus = center;
        Corners.Add(new Vector2(-1, -1), Locus);
        Corners.Add(new Vector2(1, -1), Locus);
        Corners.Add(new Vector2(-1, 1), Locus);
        Corners.Add(new Vector2(1, 1), Locus);
        
        Shape = (num, vec, corners) =>
        {
            var result = shaper(num, vec, (Dictionary<Vector2, Vector2>) corners);
            if (result.Count() == 0 || result is null)
            {
                return CellularRoomGrowerSettings.DefaultShapeChooser(new Graph(), vertex)(num, vec, (Dictionary<Vector2, Vector2>) corners);
            }
            else
            {
                return result;
            }
        };

        if (sideType == SideType.RECT)
        {
            foreach (Vector2 direction in valid_directions)
            {
                sides.Add(new Side(direction, center, Shape, Corners));
            }
        }
        else
        {
            sides.Add(new Side(Vector2.Zero, center, Shape, Corners)); // need to have modification to default shape fallback for circle
        }
        

        if (tags is not null)
        {
            Tags = tags;
        }
    }

    public IEnumerable<Vector2> GetSide(Vector2 direction)
    {
        if (sideType == SideType.RECT){
            return sides.Find((side)=>side.Direction == direction)!.Points;
        }
        else
        {
            return sides.First().Points;
        }
        
    }

    public IEnumerable<Vector2> GetSides()
    {
        return sides.SelectMany((s)=>s.Points);
    }

    public (IEnumerable<Vector2>, Dictionary<Vector2, Vector2>) GetTempGrownSides(Vector2 direction, int amount = 1)
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

    private (IEnumerable<Side>, Dictionary<Vector2, Vector2>) CalculateGrowth(Vector2 growing_side, int amount = 1)
    {
        Dictionary<Vector2, Vector2> temp_corners = new(Corners);
        

        List<Side> grown_sides_copy = new();
        // Side target_side = sides.First((s)=>s.Direction == growing_side);

        foreach (Vector2 key in Corners.Keys)
        {
            if (key.X == growing_side.X && key.X != 0)
            {
                temp_corners[key] += new Vector2(growing_side.X, 0);
            }
            else if (key.Y == growing_side.Y && key.Y != 0)
            {
                temp_corners[key] += new Vector2(0, growing_side.Y);
            }
        }

        if (growth_cache.ContainsKey(growing_side))
        {
            return (growth_cache[growing_side], temp_corners);
        }

        

        foreach (Side side in sides)
        {
            var temp_side = side.ChangeCenterBy(growing_side, temp_corners);
            temp_side = temp_side.ChangeLengthBy(amount, temp_corners);
            // if (side.Direction == growing_side)
            // {
            //     grown_sides_copy.Add(side.ChangeCenterBy(growing_side, temp_corners));
            // }
            // else if ((side.Direction + growing_side).LengthSquared() >= 1 || side.Direction == Vector2.Zero) // this might need to be adjusted for Zero
            // {
            //     Side new_side = side.ChangeLengthBy(amount, temp_corners);
                
            //     Vector2 max_y_vec = side.Points.MaxBy(v=>v.Y);
            //     Vector2 min_y_vec = side.Points.MinBy(v=>v.Y);
            //     float max_y = max_y_vec.Y;
            //     float min_y = min_y_vec.Y;
            //     Vector2 max_x_vec = side.Points.MaxBy(v=>v.X);
            //     Vector2 min_x_vec = side.Points.MinBy(v=>v.X);
            //     float max_x = max_x_vec.X;
            //     float min_x = min_x_vec.X;

            //     if (max_x == min_x && (!new_side.Points.Contains(max_y_vec + growing_side) || !new_side.Points.Contains(min_y_vec + growing_side)))
            //     {
            //         new_side = new_side.ChangeCenterBy(growing_side, temp_corners);
            //     }
            //     else if (max_y == min_y && (!new_side.Points.Contains(max_x_vec + growing_side) || !new_side.Points.Contains(min_x_vec + growing_side)))
            //     {
            //         new_side = new_side.ChangeCenterBy(growing_side, temp_corners);
            //     }
            //     else if (max_x != min_x && max_y != min_y && (min_x + growing_side.X > side.Points.Select(v=>v.X).Min() || max_x + growing_side.X < side.Points.Select(v=>v.X).Max())
            //      && (min_y + growing_side.Y > side.Points.Select(v=>v.Y).Min() || max_y + growing_side.Y < side.Points.Select(v=>v.Y).Max()))
            //     {
            //         new_side = new_side.ChangeCenterBy(growing_side, temp_corners);
            //     }

            //     grown_sides_copy.Add(new_side);

            // }
            // else
            // {
            //     grown_sides_copy.Add(side.ChangeCenterBy(growing_side, temp_corners));
            // }
            grown_sides_copy.Add(temp_side);
        }   
        growth_cache[growing_side] = grown_sides_copy;
        return (grown_sides_copy, temp_corners);
    }

    private class Side
    {
        public Vector2 Direction{get;}
        public Vector2 Center{get; private set;}
        public int Length{get; private set;}
        private Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> Shape {get;}
        public IEnumerable<Vector2> Points{get; private set;} = new List<Vector2>();

        public Side(Vector2 direction, Vector2 center, Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> shaper, Dictionary<Vector2, Vector2> corners, int initial_length=1)
        {
            CheckIfSizeIsValid(initial_length);
             
            Direction = direction;
            Center = center;
            Shape = shaper;
            Points = shaper(initial_length, direction, corners);
            Length = initial_length;
        }


        private void CheckIfSizeIsValid(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException("Sides must have length >= 1");
            }
        }

        internal Side ChangeLengthBy(int size_modifier, Dictionary<Vector2, Vector2> corners)
        {
            return SetLengthTo(Length + size_modifier, corners);
        }

        internal Side SetLengthTo(int new_size, Dictionary<Vector2, Vector2> corners)
        {
            CheckIfSizeIsValid(new_size);
            return new Side(Direction, Center, Shape, corners, new_size);
        }

        internal Side ChangeCenterBy(Vector2 center_modifier, Dictionary<Vector2, Vector2> corners)
        {
            return SetCenterTo(Center + center_modifier, corners);
        }

        internal Side SetCenterTo(Vector2 new_center, Dictionary<Vector2, Vector2> corners)
        {
            return new Side(Direction, new_center, Shape, corners, Length);
        }
    }
}

public class Hall
{
    public Vector2 Locus{get;}
    public Vertex? Vertex{get;} = null;
    public Edge? Edge{get;} = null;

    internal Hall(Vertex vertex, Vector2 center)
    {
        Vertex = vertex;
        Locus = center;
    }

    internal Hall(Edge edge, Vector2 center)
    {
        Edge = edge;
        Locus = center;
    }
}