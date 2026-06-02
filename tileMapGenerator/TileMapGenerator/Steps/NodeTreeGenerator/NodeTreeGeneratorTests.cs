namespace NodeTreeGenerator
{
    using System.Numerics;
    using System.Diagnostics;
    using MapPrimitives;
    using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
    using Vertex = MapPrimitives.RoomVertex;
    using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
    using QuikGraph.Graphviz;
    using System.Collections.Concurrent;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;
    using System.Collections.Generic;
    using System;
    using static GraphInitializer;
    using static TileMapGenerator.TarjansArticulatingPoints;
    using ConcurrentRandom;
    using Random = ConcurrentRandom.ConcurrentRandom;

    [TestClass]
    public class GraphBuilderTests
    {
        Dictionary<int, int> degree_weights = new Dictionary<int, int>
        {
            {1, 5}, 
            {2, 25}, 
            {3, 30}, 
            {4, 40}
        };
        // Appears to be deterministic
        [TestMethod]
        public void GraphBuilderTreePhase_PossibleConnectedness_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            generator.Settings.Random = new Random(112346);
            generator.Settings.Shaper = NodeTreeGeneratorSettings.RadialShaper;
            generator.Settings.InitialRatio = new Vector2(.333f, 3);
            generator.Settings.InitialPaddingPercent = 70;
            generator.Settings.PostProcessors.Add(NodeTreeGeneratorSettings.HorizontalSymmetryPostProcessor);
            generator.Settings.PostProcessors.Add(NodeTreeGeneratorSettings.VerticalSymmetryPostProcessor);   
            generator.Settings.WeightedVertexRemover = generator.Settings.AntiStrandingWeightedVertexRemover;
            generator.Settings.PruningSelectivityMultiplier = 2;
            // generator._random = new Random(13513251);
            var output = generator.GenerateNodeTree(27);
            output = generator.GenerateNodeTree(18, output);
            // (output, result) = generator.GenerateNodeTree(27, degree_weights, output);
            PrintToPNG(output, "GraphBuilderTreePhase_PossibleConnectedness_Valid");
        }

        private void CheckVerticesDegrees(Graph graph, int min, int max)
        {
            foreach (RoomVertex vert in graph.Vertices)
            {
                Assert.IsGreaterThanOrEqualTo(min, vert.Degree, $"degree too small with min: {min} and max: {max}");
                Assert.IsLessThanOrEqualTo(max, vert.Degree, $"degree too big with min: {min} and max: {max}");
            }
        }

        // Currently deterministic
        [TestMethod]
        public void NodeTreeGeneratorGenerateFilledGraph_NormalSize_Valid()
        {
            var generator = new NodeTreeGenerator();
            var(graph, dictionary) = GenerateFilledGraph(5, 5);
            PrintToPNG(graph, "NodeTreeGeneratorGenerateFilledGraph_NormalSize_Valid");
            Assert.HasCount(25, dictionary.Keys);
            Assert.HasCount(25, graph.Vertices);
            Assert.HasCount(40, graph.Edges);
            Assert.Contains(new Vector2(1, 1), dictionary.Keys);
            Assert.Contains(new Vector2(5, 5), dictionary.Keys);
            Assert.DoesNotContain(new Vector2(0,0), dictionary.Keys);
            Assert.DoesNotContain(new Vector2(6,6), dictionary.Keys);
        }

        [TestMethod]
        public void NodeTreeGeneratorTarjans_NoArticulatingPoints_Valid()
        {
            var generator = new NodeTreeGenerator();
            var(graph, dictionary) = GenerateFilledGraph(5, 5);
            var non_articulating_points = GetNonArticulatingPoints(graph, dictionary, generator.Settings.ValidDirections);
            Assert.HasCount(25, non_articulating_points);
        }

        [TestMethod]
        public void NodeTreeGeneratorTarjans_2ArticulatingPoints_Valid()
        {
            var generator = new NodeTreeGenerator();
            var(graph, dictionary) = GenerateFilledGraph(3, 3);
            var(graph2, dictionary2) = GenerateFilledGraph(3, 3);
            graph.AddVerticesAndEdge(dictionary[new Vector2(1,1)].ConnectToVertex(dictionary2[new Vector2(1, 1)], Vector2.Zero));
            graph.AddVerticesAndEdgeRange(graph2.Edges);
            var non_articulating_points = GetNonArticulatingPoints(graph, dictionary, generator.Settings.ValidDirections);
            Assert.HasCount(16, non_articulating_points);
        }

        [TestMethod]
        public void NodeTreeGeneratorTarjans_1CentralArticulatingPoint_Valid()
        {
            var generator = new NodeTreeGenerator();
            Graph graph = new Graph(false);
            ConcurrentDictionary<Vector2, Vertex> backing_dictionary = new ConcurrentDictionary<Vector2, Vertex>();
            var center = new Vertex(new Vector2(1,1));
            graph.AddVertex(center);
            backing_dictionary.TryAdd((Vector2) center.Weight, center);
            for (int i = 2; i < 12; i++)
            {
                var new_vertex = new Vertex(new Vector2(i, i));
                graph.AddVerticesAndEdge(center.ConnectToVertex(new_vertex, Vector2.Zero));
                backing_dictionary.TryAdd((Vector2) new_vertex.Weight, new_vertex);
            }
            var non_articulating_points = GetNonArticulatingPoints(graph, backing_dictionary, generator.Settings.ValidDirections);
            Assert.HasCount(10, non_articulating_points);
        }

        // Currently deterministic
        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeHalfVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.Random = new Random(4881);
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(5, 5);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 12);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeHalfVertices_Valid");
            Assert.HasCount(13, holes);
            Assert.HasCount(12, new_dictionary.Keys);
            Assert.HasCount(12, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeFifthVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(5, 5);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 5);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeFifthVertices_Valid");
            Assert.HasCount(20, holes);
            Assert.HasCount(5, new_dictionary.Keys);
            Assert.HasCount(5, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_NormalSize1Vertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(5, 5);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 1);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSize1Vertices_Valid");
            Assert.HasCount(24, holes);
            Assert.HasCount(1, new_dictionary.Keys);
            Assert.HasCount(1, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeNoVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(5, 5);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 25);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeNoVertices_Valid");
            Assert.HasCount(0, holes);
            Assert.HasCount(25, new_dictionary.Keys);
            Assert.HasCount(25, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_TinySizeHalfVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(2, 2);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 2);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_TinySizeHalfVertices_Valid");
            Assert.HasCount(2, holes);
            Assert.HasCount(2, new_dictionary.Keys);
            Assert.HasCount(2, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_OneSizeNoVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(1, 1);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 0);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_OneSizeNoVertices_Valid");
            Assert.HasCount(0, holes);
            Assert.HasCount(1, new_dictionary.Keys);
            Assert.HasCount(1, new_graph.Vertices);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_OneSizeAllVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(1, 1);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 1);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_OneSizeAllVertices_Valid");
            Assert.HasCount(0, holes);
            Assert.HasCount(1, new_dictionary.Keys);
            Assert.HasCount(1, new_graph.Vertices);
            CheckIsConnected(graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeHalfVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            generator.Settings.WeightedVertexRemover = generator.Settings.AntiStrandingWeightedVertexRemover;
            var(graph, dictionary) = GenerateFilledGraph(50, 50);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 1250);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeHalfVertices_Valid");
            Assert.HasCount(1250, holes);
            Assert.HasCount(1250, new_dictionary.Keys);
            Assert.HasCount(1250, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorDegree_HugeSizeHalfVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            generator.Settings.WeightedVertexRemover = generator.Settings.AntiStrandingWeightedVertexRemover;
            var graph = generator.GenerateNodeTree(1250);
            PrintToPNG(graph, "NodeTreeGeneratorDegree_HugeSizeHalfVertices_Valid");
            Assert.HasCount(1250, graph.Vertices);
            CheckIsConnected(graph);
            int expected_degree = (int) (1250.0*(degree_weights[1]/100.0)+1250.0*(2*degree_weights[2]/100.0)+1250.0*(3*degree_weights[3]/100.0)+1250.0*(4*degree_weights[4]/100.0));
            int actual_degree = GetGraphDegree(graph);
            Assert.AreEqual(expected_degree, actual_degree);
        }

        private int GetGraphDegree(Graph graph)
        {
            int degree = 0;
            foreach (Vertex vertex in graph.Vertices)
            {
                degree += vertex.Degree;
            }
            return degree;
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeFifthVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(50, 50);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 500);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeFifthVertices_Valid");
            Assert.HasCount(2000, holes);
            Assert.HasCount(500, new_dictionary.Keys);
            Assert.HasCount(500, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeAllButOneVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(50, 50);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 1);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeAllButOneVertices_Valid");
            Assert.HasCount(2499, holes);
            Assert.HasCount(1, new_dictionary.Keys);
            Assert.HasCount(1, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        [TestMethod]
        public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeOnlyOneVertices_Valid()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var(graph, dictionary) = GenerateFilledGraph(50, 50);
            var (new_graph, new_dictionary, holes) = generator.CutVerticesDownTo(graph, dictionary, 2499);
            PrintToPNG(new_graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeOnlyOneVertices_Valid");
            Assert.HasCount(1, holes);
            Assert.HasCount(2499, new_dictionary.Keys);
            Assert.HasCount(2499, new_graph.Vertices);
            CheckIsConnected(new_graph);
        }

        private void CheckIsConnected(Graph graph)
        {
            var checker = new QuikGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<Vertex, Edge>(graph);
            checker.Compute();
            Assert.AreEqual(1, checker.ComponentCount);
        }


        private void PrintToPNG(Graph graph, string name)
        {
            var visualizer = new GraphvizAlgorithm<Vertex, Edge>(graph);
            visualizer.FormatVertex += (_, args) =>
            {
                args.VertexFormat.Position = new QuikGraph.Graphviz.Dot.GraphvizPoint((int) args.Vertex.Weight?.X * 72, (int) args.Vertex.Weight?.Y * 72);
                args.VertexFormat.Label = args.Vertex.ID.ToString();
            };
            string file = visualizer.Generate()[..^1] + "layout=neato;\n}";
            File.WriteAllText($"../../{name}.dot", file);
            using var process = Process.Start("dot", $"-Tpng -n ../../{name}.dot -o ../../{name}.png");
            process.WaitForExit();
        }

        [TestMethod]
        public void GraphBuilderTreePhase_ConnectedUnit_Valid()
        {
            // all permutations of min and max 1 - 4, except min0-1+max=0-1
            for (Vector2 range = new Vector2(1, 4); range[0] <= 4; range += new Vector2(1, -1))
            {
                Dictionary<int, int> weights = new Dictionary<int, int>
                {
                    { (int)range[0], 10 },
                    { (int)range[1], 10 }
                };
                var tree = new NodeTreeGenerator();
                tree.Settings.degree_percents = degree_weights;
                Graph graph = tree.GenerateNodeTree(10);
                PrintToPNG(graph, $"GraphBuilderTreePhase_ConnectedUnit_Valid{range[0]}{range[1]}");
                CheckIsConnected(graph);
            
            }
        }

        [TestMethod]
        public void GraphBuilderTreePhase_RoomNumber_Valid()
        {
            for (int r = 5; r <= 100; r++)
            {
                var generator = new NodeTreeGenerator();
                generator.Settings.degree_percents = degree_weights;
                var graph = generator.GenerateNodeTree(r);
                Assert.IsInRange(r*.9, r*1.1, graph.VertexCount);
            }
        }

        [TestMethod]
        public void GraphBuilderTreePhase_SmallRoomNumbers_Valid()
        {
            for (int r = 1; r <= 4; r++)
            {
                var generator = new NodeTreeGenerator();
                generator.Settings.degree_percents = degree_weights;
                var graph = generator.GenerateNodeTree(r);
                Assert.HasCount(r, graph.Vertices);
            }
        }

        [TestMethod]
        public void GraphBuilderTreePhase_RoomNumberWithDepthDecentral_Valid()
        {
            List<int> room_input = new List<int>();
            int r = 10;
            int i = 3;   

            room_input.Clear();
            for (int j = 0; j < i; j++)
            {
                room_input.Add(r);
            }
            var generator = new NodeTreeGenerator();
            generator.Settings.degree_percents = degree_weights;
            var graph = generator.GenerateNodeTree(10);
            Assert.HasCount(r, graph.Vertices);
        }

        [TestMethod]
        public void GraphBuilderTreePhase_NormalDepthStressTest_Valid()
        {
            for (int depth = 1; depth <= 7; depth++)
            {   
                var generator = new NodeTreeGenerator();
                generator.Settings.degree_percents = degree_weights;
                var graph = generator.GenerateNodeTree(5+depth);
                CheckIsConnected(graph);
                Assert.HasCount(5+depth, graph.Vertices);
            }
        }

        [Timeout(10000)]
        [TestMethod]
        public void GraphBuilderTreePhase_SpeedConnected_Valid()
        {
            for (int i = 1; i <= 1000; i++)
            {
                int r = (i % 101)+1;
                var generator = new NodeTreeGenerator();
                generator.Settings.degree_percents = degree_weights;
                generator.Settings.Random = new Random(r*13*17+1%(i*i));
                var graph = generator.GenerateNodeTree(r);
                CheckIsConnected(graph);
            }
        }

        [TestMethod]
        public void GraphBuilderTreePhase_WeightsCardinalDiagonal_Valid()
        {
            List<Vector2> directions = new List<Vector2>(){Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY};
            for (Vector2 range = new Vector2(1, 4); range[0] <= 4; range += new Vector2(1, -1))
            {
                Dictionary<int, int> weights = new Dictionary<int, int>
                {
                    { (int)range[0], 10 },
                    { (int)range[1], 10 }
                };
                for (int i = 0; i < 4; i++)
                {
                    var generator = new NodeTreeGenerator();
                    generator.Settings.degree_percents = degree_weights;
                    var graph = generator.GenerateNodeTree(40);
                    var vertices = graph.Vertices;
                    foreach (var vertex in vertices)
                    {
                        foreach (Edge edge in vertex.Edges)
                        {
                            Assert.Contains(edge.Weight, directions);
                        }
                    }
                }

            }
        }
    }
}