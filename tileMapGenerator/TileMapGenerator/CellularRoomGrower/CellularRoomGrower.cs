namespace CellularRoomGrower;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;

public class CellularRoomGrower
{


    public CellularRoomGrowerSettings Settings{get; set;} = new CellularRoomGrowerSettings();

    private Vector2 old_relative_size;

    public (Graph, BinaryGrid, IEnumerable<Room>, IEnumerable<Hall>) GenerateSizedRooms(Graph graph, int map_area)
    {
        Settings.MapArea = map_area;
        old_relative_size = GetOldRelativeSize(graph);
        IEnumerable<Room> rooms = BuildRooms(graph);
        IEnumerable<Hall> halls = BuildHalls(graph);
        BinaryGrid grid = BuildGrid(rooms, halls);
        (rooms, grid) = GrowRooms(rooms, graph, grid, Settings.Prioritizer);
        return (graph, grid, rooms, halls);
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
            Room new_room = new Room(vertex, Settings.ShapeChooser(graph, vertex), Settings.ValidDirections, AdjustLocus(vertex.Weight), tags);
            rooms.Add(new_room);
        }
        return rooms;
    }

    internal IEnumerable<Hall> BuildHalls(Graph graph)
    {
        List<Hall> halls = new(graph.EdgeCount);
        foreach (Edge edge in graph.Edges)
        {
            halls.Add(new Hall(edge, AdjustLocus((edge.Source.Weight + edge.Target.Weight) * new Vector2(.5f, .5f))));
        }
        return halls;
    }

    // Essentially stretches the vector2 according to a matrix transformation
    private Vector2 AdjustLocus(Vector2 locus)
    {
        double width_modifier = (Math.Sqrt(Settings.MapArea) * Settings.SideRatio.X - 2.0) / old_relative_size.X;
        double height_modifier = (Math.Sqrt(Settings.MapArea) * Settings.SideRatio.Y - 2.0) / old_relative_size.Y;
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
        double width = Math.Ceiling(Math.Sqrt(Settings.MapArea) * Settings.SideRatio.X + 1.0);
        double height = Math.Ceiling(Math.Sqrt(Settings.MapArea) * Settings.SideRatio.Y + 1.0);
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
        List<Room> new_rooms = new(rooms.Count());
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
                if (CheckDirection(grid, direction, room))
                {
                    growable_rooms.Add(room);
                    break;  
                }
            }
        }
        return growable_rooms;
    }

    private (BinaryGrid, Room) GrowRoom(BinaryGrid grid, Room room, Func<IEnumerable<Vector2>, Room, Vector2> direction_chooser)
    {
        List<Vector2> open_directions = new();
        foreach (Vector2 direction in Settings.ValidDirections)
        {
            if (CheckDirection(grid, direction, room))
            {
                open_directions.Add(direction);
            }
        }

        
        if (open_directions is null)
        {
            throw new NullReferenceException("Room cannot grow, no directions found");
        }

        Vector2 chosen_direction = direction_chooser(open_directions, room);
        (grid, room) = GrowSide(grid, room, chosen_direction);
        return (grid, room);
    }

    private bool CheckDirection(BinaryGrid grid, Vector2 direction, Room room)
    {
        int side_check = (int) room.GetTempGrownSides(direction).Select(vec=>grid.GetCell((uint)vec.Y, (uint)vec.X)).Sum(i=>i);
        return side_check == 0;
    }

    private (BinaryGrid, Room) GrowSide(BinaryGrid grid, Room room, Vector2 side)
    {
        foreach (Vector2 point in room.GetTempGrownSides(side))
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
    public Func<int, Vector2, IEnumerable<Vector2>> Shape {get; private set;}
    private List<Vector2> corners = new();
    private List<Side> sides = new();
    public IEnumerable<string> Tags{get; set;} = new List<string>();
    

    internal Room(Vertex vertex, Func<int, Vector2, IEnumerable<Vector2>> shaper, IEnumerable<Vector2> valid_directions, Vector2 center, IEnumerable<string>? tags = null)
    {
        Vertex = vertex;
        Locus = center;
        Shape = shaper;
        corners.Add(center);
        foreach (Vector2 direction in valid_directions)
        {
            sides.Add(new Side(direction, center+direction, shaper));
        }

        if (tags is not null)
        {
            Tags = tags;
        }
    }

    public IEnumerable<Vector2> GetSide(Vector2 direction)
    {
        return sides.Find((side)=>side.Direction == direction)!.Points;
    }

    public IEnumerable<Vector2> GetTempGrownSides(Vector2 direction, int amount = 1)
    {
        return CalculateGrowth(direction, amount).SelectMany((side)=>side.Points);
    }

    public void GrowSide(Vector2 direction, int amount = 1)
    {
        IEnumerable<Side> new_sides = CalculateGrowth(direction, amount);
        sides = new List<Side>(new_sides);
    }

    private IEnumerable<Side> CalculateGrowth(Vector2 growing_side, int amount = 1)
    {
        List<Side> grown_sides_copy = new();

        foreach (Side side in sides)
        {
            if (side.Direction == growing_side)
            {
                grown_sides_copy.Add(side.ChangeCenterBy(growing_side));
            }
            else if ((side.Direction + growing_side).LengthSquared() >= 1)
            {
                grown_sides_copy.Add(side.ChangeLengthBy(amount));
            }
            else
            {
                grown_sides_copy.Add(side);
            }
        }   

        return grown_sides_copy;
    }

    private class Side
    {
        public Vector2 Direction{get;}
        public Vector2 Center{get; private set;}
        public int Length{get; private set;}
        private Func<int, Vector2, IEnumerable<Vector2>> Shape {get;}
        public IEnumerable<Vector2> Points{get; private set;} = new List<Vector2>();

        public Side(Vector2 direction, Vector2 center, Func<int, Vector2, IEnumerable<Vector2>> shaper, int initial_length=1)
        {
            CheckIfSizeIsValid(initial_length);
             
            Direction = direction;
            Center = center;
            Shape = shaper;
            Points = OffsetPoints(shaper(initial_length, direction), center);
            Length = initial_length;
        }

        private IEnumerable<Vector2> OffsetPoints(IEnumerable<Vector2> points, Vector2 center)
        {
            List<Vector2> offset_points = new();
            foreach (Vector2 point in points)
            {
                offset_points.Add(point + center);
            }
            return offset_points;
        }

        private void CheckIfSizeIsValid(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException("Sides must have length >= 1");
            }
        }

        internal Side ChangeLengthBy(int size_modifier)
        {
            return SetLengthTo(Length + size_modifier);
        }

        internal Side SetLengthTo(int new_size)
        {
            CheckIfSizeIsValid(new_size);
            return new Side(Direction, Center, Shape, new_size);
        }

        internal Side ChangeCenterBy(Vector2 center_modifier)
        {
            return SetCenterTo(Center + center_modifier);
        }

        internal Side SetCenterTo(Vector2 new_center)
        {
            return new Side(Direction, new_center, Shape, Length);
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