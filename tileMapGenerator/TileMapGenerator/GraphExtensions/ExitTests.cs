namespace Exits
{
    using Grid;
    using Graph = QuikGraph.UndirectedGraph<Primitives.ZVertex<System.Numerics.Vector2>, Primitives.ZEdge<System.Numerics.Vector2>>;
    using Vertex = Primitives.ZVertex<System.Numerics.Vector2>;
    using Edge = Primitives.ZEdge<System.Numerics.Vector2>;
    using System.Numerics;
    using NodeTreeGenerator;
    using System.Diagnostics;
    using QuikGraph.Graphviz;
    using Primitives;
    using CellularGrower;
    using GraphExtensions;
    using HallMaker;
    using Vector2Extensions;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class ExitStamperTests
    {

        Dictionary<int, int> degree_weights = new Dictionary<int, int>
        {
            {1,10}, {2, 30}, {3, 30}, {4, 35}
            
        };

        [TestMethod]
        public void ExitStamper_CorrectCounts30_Valid()
        {
            var room_grower = new CellularGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            CellularGrowerSettings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            Graph graph = generator.GenerateNodeTree(30);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateZones(graph, 30*30);
            (_, grid, rooms, halls) = new HallMaker().GenerateHalls(new_graph, grid, rooms, halls);
            var exits = ExitStamper.ChooseExits(rooms, halls);
            var patches = ExitStamper.PatchFrames(exits, grid);
            grid.ToBMP("ExitStamper_CorrectCounts30_Valid");
            Grid exit_grid = new Grid(grid.NRows, grid.NCols);
            foreach (var point in exits)
            {
                exit_grid.SetCell(point.Reverse(), 1);
            }
            foreach (var point in patches)
            {
                exit_grid.SetCell(point.Reverse(), 1);
            }
            exit_grid.ToBMP("ExitStamper_CorrectCounts30_Valid", "0xFFFF0000", true);
        }


        [TestMethod]
        public void ExitStamper_CircularCorrectCounts30_Valid()
        {
            var room_grower = new CellularGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(6312313);
            generator.Settings.degree_percents = degree_weights;
            room_grower.Settings.ShapeChooser = CellularGrowerSettings.CircularShapeChooser;
            CellularGrowerSettings.Random = new Random(134534); 
            Graph graph = generator.GenerateNodeTree(25);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateZones(graph, 30*30);
            (_, grid, rooms, halls) = new HallMaker().GenerateHalls(new_graph, grid, rooms, halls);
            var exits = ExitStamper.ChooseExits(rooms, halls);
            var patches = ExitStamper.PatchFrames(exits, grid);
            grid.ToBMP("ExitStamper_CircularCorrectCounts30_Valid");
            Grid exit_grid = new Grid(grid.NRows, grid.NCols);
            foreach (var point in exits)
            {
                exit_grid.SetCell(point.Reverse(), 1);
            }
            foreach (var point in patches)
            {
                exit_grid.SetCell(point.Reverse(), 1);
            }
            exit_grid.ToBMP("ExitStamper_CircularCorrectCounts30_Valid", "0xFFFF0000", true);

        }

        [TestMethod]
        public void ExitStamper_CaveCorrectCounts30_Valid()
        {
            var room_grower = new CellularGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            room_grower.Settings.ShapeChooser = CellularGrowerSettings.CaveShapeChooser;
            CellularGrowerSettings.Random = new Random(134534); 
            Graph graph = generator.GenerateNodeTree(25);
        
            var (new_graph, grid, rooms, halls) = room_grower.GenerateZones(graph, 30*30);
            (_, grid, rooms, halls) = new HallMaker().GenerateHalls(new_graph, grid, rooms, halls);
            var exits = ExitStamper.ChooseExits(rooms, halls);
            var patches = ExitStamper.PatchFrames(exits, grid);
            grid.ToBMP("ExitStamper_CaveCorrectCounts30_Valid");
            Grid exit_grid = new Grid(grid.NRows, grid.NCols);
            foreach (var point in exits)
            {
                exit_grid.SetCell(point.Reverse(), 1);
            }
            foreach (var point in patches)
            {
                exit_grid.SetCell(point.Reverse(), 1);
            }
            exit_grid.ToBMP("ExitStamper_CaveCorrectCounts30_Valid", "0xFFFF0000", true);
        }
    }
}