namespace CellularRoomGrower
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
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.IO;

    [TestClass]
    public class CellularRoomGrowerTests
    {
        Dictionary<int, int> degree_weights = new Dictionary<int, int>()
        {
            {1, 10},
            {2, 30}, 
            {3, 30}, 
            {4, 35}
        };

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
            var (_, grid, _, _) = room_grower.GenerateSizedRooms(graph, 1000);
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_Runs_Valid");
        }

        [TestMethod]
        public void CellularRoomGrowerGenerateSizedRooms_ExactSize_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            var(graph, _) = generator.GenerateFilledGraph(5, 5);
            var (_, grid, _, _) = room_grower.GenerateSizedRooms(graph, 65);
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_ExactSize_Valid");
        }

        [TestMethod]
        public void CellularRoomGrowerGenerateSizedRooms_CorrectCounts5_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            var graph = generator.GenerateNodeTree(5);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*5);
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CorrectCounts5_Valid");
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
            var graph = generator.GenerateNodeTree(1);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*1);
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CorrectCounts1_Valid");
            Assert.HasCount(graph.VertexCount, rooms, "Room count 1 failed");
            Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 1 failed");
            Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 1 failed");
            Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 1 failed");
            Assert.HasCount(graph.EdgeCount, halls, "Hall count 1 failed");
        }

        [TestMethod]
        public void CellularRoomGrowerGrowRoom_CorrectGrowth1Room_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            var graph = generator.GenerateNodeTree(1);
            var vertex = new Vertex(new Vector2(10, 10));
            var room = new Room(vertex, CellularRoomGrowerSettings.DefaultShapeChooser(graph, vertex, new Vector2(10, 10)), CellularRoomGrowerSettings.DefaultValidDirections, new Vector2(10, 10));
            BinaryGrid grid = new BinaryGrid(100, 100);
            new CellularRoomGrower().GrowRoom(grid, room, (a, _)=>new Vector2(1, 0));
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow1");
            new CellularRoomGrower().GrowRoom(grid, room, (a, _)=>new Vector2(-1, 0));
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow2");
            new CellularRoomGrower().GrowRoom(grid, room, (a, _)=>new Vector2(0, -1));
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow3");
            new CellularRoomGrower().GrowRoom(grid, room, (a, _)=>new Vector2(0, 1));
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow4");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow5");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow6");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow7");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow8");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow9");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow10");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow11");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow12");
            new CellularRoomGrower().GrowRoom(grid, room, CellularRoomGrowerSettings.DefaultDirectionChooser);
            grid.ToBMP("CellularRoomGrowerGrowRoom_CorrectGrowth1Room_ValidGrow13");
            // Assert.AreEqual(2, grid.GetSlice((uint)))
        }

        [TestMethod]
        public void CellularRoomGrowerGenerateSizedRooms_CorrectCounts30_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            CellularRoomGrowerSettings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            Graph graph = generator.GenerateNodeTree(30);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CorrectCounts30_Valid");

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
            Assert.Throws<Exception> (()=>room_grower.GenerateSizedRooms(graph, 30*1));
        }

        [TestMethod]
        public void CellularRoomGenerateSizedRooms_TooSmallToFit_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            var(graph, _) = generator.GenerateFilledGraph(5, 5);
            var (_, grid, rooms, _) = room_grower.GenerateSizedRooms(graph, 30);
            grid.ToBMP("CellularRoomGenerateSizedRooms_TooSmallToFit_Valid");

            Assert.HasCount(graph.VertexCount, rooms, "size 30");
            (_, _, rooms, _) = room_grower.GenerateSizedRooms(graph, 64);
            Assert.HasCount(graph.VertexCount, rooms, "size 64");
        }

        // [Timeout(10000)]
        // [TestMethod]
        // public void CellularRoomGenerateSizedRooms_EmptyHelperMethods_Valid()
        // {
        //     var room_grower = new CellularRoomGrower();
        //     var generator = new NodeTreeGenerator();
        //     generator.Settings.Random = new Random(123);
        //     generator.Settings.degree_percents = degree_weights;
        //     var(graph, _) = generator.GenerateFilledGraph(5, 5);
        //     room_grower.Settings.DirectionChooser = (_, _)=>Vector2.Zero;
        //     var (_, _, rooms, _) = room_grower.GenerateSizedRooms(graph, 250);
        //     Assert.HasCount(25, rooms, "Bad Direction Chooser case");
        //     room_grower.Settings.DirectionChooser = CellularRoomGrowerSettings.DefaultDirectionChooser;
        //     room_grower.Settings.ShapeChooser = (_, _, _)=>(_, _, _)=>[];
        //     (graph, _) = generator.GenerateFilledGraph(5, 5);
        //     (_, _, rooms, _) = room_grower.GenerateSizedRooms(graph, 250);
        //     Assert.HasCount(25, rooms, "Bad Shape Chooser case");
        //     room_grower.Settings.ShapeChooser = CellularRoomGrowerSettings.DefaultShapeChooser;
        //     room_grower.Settings.ValidDirections = [];
        //     (graph, _) = generator.GenerateFilledGraph(5, 5);
        //     (_, _, rooms, _) = room_grower.GenerateSizedRooms(graph, 250);
        //     Assert.HasCount(25, rooms, "Bad Valid Directions case");
        //     room_grower.Settings.ValidDirections =  CellularRoomGrowerSettings.DefaultValidDirections;
        //     room_grower.Settings.SideRatio = Vector2.Zero;
        //     (graph, _) = generator.GenerateFilledGraph(5, 5);
        //     (_, _, rooms, _) = room_grower.GenerateSizedRooms(graph, 250);
        //     Assert.HasCount(25, rooms, "Bad Side Ratio case");
        // }

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
            var graph = generator.GenerateNodeTree(5);
            room_grower.Settings.MapArea = 30 * 5;
            var halls = room_grower.BuildHalls(graph);
            Assert.HasCount(graph.EdgeCount, halls);
        }

        [TestMethod]
        public void CellularRoomGrowerBuildGrid_FilledGridSpotsCount_Valid()
        {
            var room_grower = new CellularRoomGrower();
            room_grower.Settings.MapArea = 300 * 5;
            List<Room> rooms = new List<Room>(){new Room(new Vertex(), room_grower.Settings.ShapeChooser(new Graph(), new Vertex(), Vector2.Zero), room_grower.Settings.ValidDirections, Vector2.One)};
            for (int i = 0; i < 5; i++)
            {
                rooms.Add(new Room(new Vertex(), room_grower.Settings.ShapeChooser(new Graph(), new Vertex(), Vector2.Zero), room_grower.Settings.ValidDirections, new Vector2((i+2)*5, (i+2)*5)));
            }
            List<Hall> halls = new List<Hall>();
            for (int i = 0; i < rooms.Count -1; i++)
            {
                halls.Add(new Hall(new Edge(rooms[i].Vertex, rooms[i+1].Vertex), ((rooms[i].Locus + rooms[i+1].Locus)/2), (Vector2) rooms[i].Vertex.Weight!, (Vector2)  rooms[i+1].Vertex.Weight!));
            }

            var grid = room_grower.BuildGrid(rooms, halls);

            Assert.AreEqual(rooms.Count + halls.Count, GetGridCount(grid));
        }

        [TestMethod]
        public void CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid()
        {
            var room_grower = new CellularRoomGrower();
            room_grower.Settings.ShapeChooser = CellularRoomGrowerSettings.CircularShapeChooser;
            room_grower.Settings.SideRatio = new Vector2(.5f, 2);
            var generator = new NodeTreeGenerator();
            // generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            // Graph graph = generator.GenerateNodeTree(30);
            Graph graph = generator.GenerateNodeTree(1);

            // var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid");

            BinaryGrid grid = new BinaryGrid(100, 100);
            var vertex = new Vertex(new Vector2(10, 10));
            var room = new Room(vertex, CellularRoomGrowerSettings.CircularShapeChooser(graph, vertex, new Vector2(10, 10)), CellularRoomGrowerSettings.DefaultValidDirections, new Vector2(10, 10));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(-1, 0));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid1");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid2");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid3");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid4");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(-1, 0));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid5");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(-1, 0));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid6");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(-1, 0));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid7");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(1, 0));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid8");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid9");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(-1, 0));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid10");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(-1, 0));
            // grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid11");
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(1, 0));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(1, 0));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(1, 0));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(1, 0));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(1, 0));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(1, 0));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
            // (grid, room) = new CellularRoomGrower().GrowRoom(grid, room, (_, _)=>new Vector2(0, 1));
        
        
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularShaper_Valid12");

            // Assert.HasCount(graph.VertexCount, rooms, "Room count 30 failed");
            // Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 30 failed");
            // Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 30 failed");
            // Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 30 failed");
            // Assert.HasCount(graph.EdgeCount, halls, "Hall count 30 failed");
        }

        [TestMethod]
        public void CellularRoomGrowerGenerateSizedRooms_CircularCorrectCounts30_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(6312313);
            generator.Settings.degree_percents = degree_weights;
            room_grower.Settings.ShapeChooser = CellularRoomGrowerSettings.CircularShapeChooser;
            CellularRoomGrowerSettings.Random = new Random(134534); 
            Graph graph = generator.GenerateNodeTree(25);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CircularCorrectCounts30_Valid");

            Assert.HasCount(graph.VertexCount, rooms, "Room count 30 failed");
            Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 30 failed");
            Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 30 failed");
            Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 30 failed");
            Assert.HasCount(graph.EdgeCount, halls, "Hall count 30 failed");
        }

        [TestMethod]
        public void CellularRoomGrowerGenerateSizedRooms_CaveCorrectCounts30_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            room_grower.Settings.ShapeChooser = CellularRoomGrowerSettings.CaveShapeChooser;
            CellularRoomGrowerSettings.Random = new Random(134534); 
            Graph graph = generator.GenerateNodeTree(25);
            // PrintGraphToPNG(graph, "Caves");
        
            var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30*30);
            grid.ToBMP("CellularRoomGrowerGenerateSizedRooms_CaverCorrectCounts30_Valid");

            Assert.HasCount(graph.VertexCount, rooms, "Room count 30 failed");
            Assert.HasCount(graph.VertexCount, new_graph.Vertices, "New Graph vertices count 30 failed");
            Assert.IsGreaterThan(graph.VertexCount+graph.EdgeCount, GetGridCount(grid), "Grid cells count 30 failed");
            Assert.HasCount(graph.EdgeCount, new_graph.Edges, "New graph edge count 30 failed");
            Assert.HasCount(graph.EdgeCount, halls, "Hall count 30 failed");
        }

        [TestMethod]
        public void RandomCurrent()
        {
            var rand = new ConcurrentRandom.ConcurrentRandom(134534);
            for (int i = 0; i < 1000; i++)
            {
                Console.WriteLine("rand:" + rand.Next(i, 0, 160));
                Console.WriteLine("vec:" + rand.NextVector2(i, 0, 160, 0, 160));
            }
        }

        [TestMethod]
        public void CellularRoomGrowerGrowRooms_MaxSizeUsed_Valid()
        {
            var room_grower = new CellularRoomGrower();
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            var graph = generator.GenerateNodeTree(5);
            var (new_graph, grid, rooms, halls) = room_grower.GenerateSizedRooms(graph, 30 * 5);
            grid.ToBMP("CellularRoomGrowerGrowRooms_MaxSizeUsed_Valid");
            for (uint i = 1; i < grid.RowSize; i++)
            {
                Assert.IsGreaterThan(0u, grid.GetSliceOR(i, 1, i, grid.ColSize));
            }

            for (uint j = 1; j < grid.RowSize; j++)
            {
                Assert.IsGreaterThan(0u, grid.GetSliceOR(1, j, grid.RowSize, j));
            }
        }

        private Graph InitializeGraph()
        {
            NodeTreeGenerator generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(123);
            generator.Settings.degree_percents = degree_weights;
            var graph = generator.GenerateNodeTree(5);
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

        private void PrintGraphToPNG(Graph graph, string name)
        {
            var visualizer = new GraphvizAlgorithm<Vertex, Edge>(graph);
            visualizer.FormatVertex += (_, args) =>
            {
                args.VertexFormat.Position = new QuikGraph.Graphviz.Dot.GraphvizPoint((int) args.Vertex.Weight?.X! * 72, (int) args.Vertex.Weight?.Y! * 72);
                // args.VertexFormat.Label = args.Vertex.VertexID.ToString();
            };
            string file = visualizer.Generate()[..^1] + "layout=neato;\n}";
            File.WriteAllText($"../../{name}.dot", file);
            using var process = Process.Start("dot", $"-Tpng -n ../../{name}.dot -o ../../{name}.png");
            process.WaitForExit();
        }
    }
}