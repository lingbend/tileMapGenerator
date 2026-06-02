namespace DoorChoser
{
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
    using HallMaker;
    using Vector2Extensions;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class DoorChoserTests
    {

        Dictionary<int, int> degree_weights = new Dictionary<int, int>
        {
            {1,10}, {2, 30}, {3, 30}, {4, 35}
            
        };

        [TestMethod]
        public void DoorChoser_CorrectCounts30_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            CellularRoomGrowerSettings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            Graph graph = generator.GenerateNodeTree(30);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
            (_, grid, rooms, halls) = new HallMaker().GenerateHalls(new_graph, grid, rooms, halls);
            var doors = DoorChoser.ChooseDoors(rooms, halls);
            var patches = DoorChoser.PatchDoorframes(doors, grid);
            grid.ToBMP("DoorChoser_CorrectCounts30_Valid");
            BinaryGrid door_grid = new BinaryGrid(grid.RowSize, grid.ColSize);
            foreach (var point in doors)
            {
                door_grid.SetCell(point.Reverse(), 1);
            }
            foreach (var point in patches)
            {
                door_grid.SetCell(point.Reverse(), 1);
            }
            door_grid.ToBMP("DoorChoser_CorrectCounts30_Valid", "0xFFFF0000", true);
        }


        [TestMethod]
        public void DoorChoser_CircularCorrectCounts30_Valid()
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
            var doors = DoorChoser.ChooseDoors(rooms, halls);
            var patches = DoorChoser.PatchDoorframes(doors, grid);
            grid.ToBMP("DoorChoser_CircularCorrectCounts30_Valid");
            BinaryGrid door_grid = new BinaryGrid(grid.RowSize, grid.ColSize);
            foreach (var point in doors)
            {
                door_grid.SetCell(point.Reverse(), 1);
            }
            foreach (var point in patches)
            {
                door_grid.SetCell(point.Reverse(), 1);
            }
            door_grid.ToBMP("DoorChoser_CircularCorrectCounts30_Valid", "0xFFFF0000", true);

        }

        [TestMethod]
        public void DoorChoser_CaveCorrectCounts30_Valid()
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
            var doors = DoorChoser.ChooseDoors(rooms, halls);
            var patches = DoorChoser.PatchDoorframes(doors, grid);
            grid.ToBMP("DoorChoser_CaveCorrectCounts30_Valid");
            BinaryGrid door_grid = new BinaryGrid(grid.RowSize, grid.ColSize);
            foreach (var point in doors)
            {
                door_grid.SetCell(point.Reverse(), 1);
            }
            foreach (var point in patches)
            {
                door_grid.SetCell(point.Reverse(), 1);
            }
            door_grid.ToBMP("DoorChoser_CaveCorrectCounts30_Valid", "0xFFFF0000", true);
        }
    }
}