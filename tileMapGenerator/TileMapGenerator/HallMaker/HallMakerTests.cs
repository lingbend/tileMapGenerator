namespace HallMaker;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex<System.Numerics.Vector2>, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
using Vertex = MapPrimitives.RoomVertex<System.Numerics.Vector2>;
using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using NodeTreeGenerator;
using System.Diagnostics;
using QuikGraph.Graphviz;
using MapPrimitives;
using CellularRoomGrower;
using GraphExtensions;

[TestClass]
public class HallMakerTests
{

    Dictionary<int, int> degree_weights = new Dictionary<int, int>([KeyValuePair.Create(1, 10), KeyValuePair.Create(2, 30), KeyValuePair.Create(3, 30), KeyValuePair.Create(4, 35)]);

    [TestMethod]
    public void HallMakerGenerateHalls_CorrectCounts30_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        CellularRoomGrowerSettings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        Graph graph = generator.GenerateNodeTree(30);
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
        (_, grid, rooms, halls) = new HallMaker().GenerateHalls(new_graph, grid, rooms, halls);
        graph.PrintToPNG("HallMakerGenerateHalls_CorrectCounts30_Valid-Graph");
        grid.ToBMP("HallMakerGenerateHalls_CorrectCounts30_Valid-Grid");
    }

    [TestMethod]
    public void HallMakerGenerateHalls_Count2Separate_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        CellularRoomGrowerSettings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        Graph graph = generator.GenerateNodeTree(2);
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
        var temp_grid = new HallMaker().GenerateOnlyHalls(new_graph, grid, rooms, halls);
        grid.ToBMP("HallMakerGenerateHalls_Count2_Valid");
        temp_grid.ToBMP("HallMakerGenerateHalls_Count2_ValidHalls");
    }


    [TestMethod]
    public void HallMakerGenerateHalls_CircularCorrectCounts30_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(6312313);
        generator.Settings.degree_percents = degree_weights;
        room_grower.Settings.ShapeChooser = CellularRoomGrowerSettings.CircularShapeChooser;
        CellularRoomGrowerSettings.Random = new Random(134534); 
        Graph graph = generator.GenerateNodeTree(25);
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
        (_, grid, rooms, halls) = new HallMaker().GenerateHalls(new_graph, grid, rooms, halls);
        grid.ToBMP("HallMakerGenerateHalls_CircularCorrectCounts30_Valid");

    }

    [TestMethod]
    public void HallMakerGenerateHalls_CaveCorrectCounts30_Valid()
    {
        var room_grower = new CellularRoomGrower();
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(123);
        generator.Settings.degree_percents = degree_weights;
        room_grower.Settings.ShapeChooser = CellularRoomGrowerSettings.CaveShapeChooser;
        CellularRoomGrowerSettings.Random = new Random(134534); 
        Graph graph = generator.GenerateNodeTree(25);
        
        var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
        (_, grid, rooms, halls) = new HallMaker().GenerateHalls(new_graph, grid, rooms, halls);
        grid.ToBMP("HallMakerGenerateHalls_CaveCorrectCounts30_Valid");
    }
}