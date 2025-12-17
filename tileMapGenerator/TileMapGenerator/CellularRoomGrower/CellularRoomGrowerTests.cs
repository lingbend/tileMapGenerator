namespace CellularRoomGrower;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using NodeTreeGenerator;
using System.Diagnostics;

[TestClass]
public class CellularRoomGrowerTests
{
    Dictionary<int, int> degree_weights = new Dictionary<int, int>([KeyValuePair.Create(1, 10), KeyValuePair.Create(2, 30), KeyValuePair.Create(3, 30), KeyValuePair.Create(4, 35)]);

    [TestMethod]
    public void CellularRoomGrowerConstructor_Valid()
    {
        _ = new CellularRoomGrower();
    }

    [TestMethod]
    public void CellularRoomGrowerGenerateSizedRooms_Runs_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        var(graph, _) = generator.GenerateFilledGraph(5, 5);
        room_grower.GenerateSizedRooms(graph, 100);
    }

    [TestMethod]
    public void CellularRoomGrowerGenerateSizedRooms_CorrectCounts5_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        var(graph, _) = generator.GenerateNodeTree(5, degree_weights);
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*5);
        Assert.HasCount(graph.VertexCount, rooms, "Room count 5 failed");
        Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 5 failed");
        Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 5 failed");
        Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 5 failed");
        Assert.HasCount(graph.EdgeCount, halls, "Hall count 5 failed");
    }

    [TestMethod]
    public void CellularRoomGrowerGenerateSizedRooms_CorrectCounts1_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        var(graph, _) = generator.GenerateNodeTree(1, degree_weights);
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*1);
        Assert.HasCount(graph.VertexCount, rooms, "Room count 1 failed");
        Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 1 failed");
        Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 1 failed");
        Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 1 failed");
        Assert.HasCount(graph.EdgeCount, halls, "Hall count 1 failed");
    }

    [TestMethod]
    public void CellularRoomGrowerGenerateSizedRooms_CorrectCounts30_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        var(graph, _) = generator.GenerateNodeTree(30, degree_weights);
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
        Assert.HasCount(graph.VertexCount, rooms, "Room count 30 failed");
        Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 30 failed");
        Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 30 failed");
        Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 30 failed");
        Assert.HasCount(graph.EdgeCount, halls, "Hall count 30 failed");
    }

    [TestMethod]
    public void CellularRoomGenerateSizedRooms_0Rooms_Invalid()
    {
        var room_grower = new CellularRoomGrower();
        Graph graph = new Graph();
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*1);
        Assert.HasCount(graph.VertexCount, rooms, "Room count 30 failed");
        Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 30 failed");
        Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 30 failed");
        Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 30 failed");
        Assert.HasCount(graph.EdgeCount, halls, "Hall count 30 failed");
    }

    [TestMethod]
    public void CellularRoomGenerateSizedRooms_TooSmallToFit_Invalid()
    {
        
    }

    [TestMethod]
    public void CellularRoomGenerateSizedRooms_EmptyHelperMethods_Invalid()
    {
        
    }

    [TestMethod]
    public void CellularRoomGrowerBuildRooms_RoomCount_Valid()
    {
        var room_grower = new CellularRoomGrower();
        Graph graph = InitializeGraph();
        room_grower.Settings.MapArea = 30 * 5;
        var rooms = room_grower.BuildRooms(graph);
        Assert.HasCount(graph.VertexCount, rooms);
    }

    [TestMethod]
    public void CellularRoomGrowerBuildHalls_HallCount_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        var(graph, _) = generator.GenerateNodeTree(5, degree_weights);
        room_grower.Settings.MapArea = 30 * 5;
        var halls = room_grower.BuildHalls(graph);
        Assert.HasCount(graph.EdgeCount, halls);
    }

    [TestMethod]
    public void CellularRoomGrowerBuildGrid_FilledGridSpotsCount_Valid()
    {
        var room_grower = new CellularRoomGrower();
        room_grower.Settings.MapArea = 300 * 5;
        List<Room> rooms = new(){new Room(new Vertex(), room_grower.Settings.ShapeChooser(new Graph(), new Vertex()), room_grower.Settings.ValidDirections, Vector2.One)};
        for (int i = 0; i < 5; i++)
        {
            rooms.Add(new Room(new Vertex(), room_grower.Settings.ShapeChooser(new Graph(), new Vertex()), room_grower.Settings.ValidDirections, new Vector2((i+2)*5, (i+2)*5)));
        }
        List<Hall> halls = new();
        for (int i = 0; i < rooms.Count -1; i++)
        {
            halls.Add(new Hall(new Edge(rooms[i].Vertex, rooms[i+1].Vertex), ((rooms[i].Locus + rooms[i+1].Locus)/2)));
        }

        var grid = room_grower.BuildGrid(rooms, halls);
        Assert.AreEqual(rooms.Count + halls.Count, GetGridCount(grid));
    }

    [TestMethod]
    public void CellularRoomGrowerGrowRooms_Valid()
    {
        
    }

    private Graph InitializeGraph()
    {
        NodeTreeGenerator generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        var(graph, _) = generator.GenerateNodeTree(5, degree_weights);
        return graph;
    }

    private int GetGridCount(BinaryGrid grid)
    {
        uint total = 0;
        for (uint col = 1; col <= grid.ColSize; col++)
        {
            for (uint row = 1; row <= grid.RowSize; row++)
            {
                total += grid.GetCell(row, col);
            }
        }
        return (int) total;
    }
}