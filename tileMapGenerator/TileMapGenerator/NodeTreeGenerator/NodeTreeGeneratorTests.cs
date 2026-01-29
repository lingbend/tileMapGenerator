namespace NodeTreeGenerator;

using System.Numerics;
using System.Diagnostics;
using MapPrimatives;
using Graph = QuikGraph.UndirectedGraph<MapPrimatives.RoomVertex<System.Numerics.Vector2>, MapPrimatives.RoomEdge<System.Numerics.Vector2>>;
using Vertex = MapPrimatives.RoomVertex<System.Numerics.Vector2>;
using Edge = MapPrimatives.RoomEdge<System.Numerics.Vector2>;
using QuikGraph.Graphviz;
using System.Collections.Concurrent;

[TestClass]
public class GraphBuilderTests
{
    Dictionary<int, int> degree_weights = new Dictionary<int, int>([KeyValuePair.Create(1, 5), KeyValuePair.Create(2, 25), KeyValuePair.Create(3, 30), KeyValuePair.Create(4, 40)]);

    // Appears to be deterministic
    [TestMethod]
    public void GraphBuilderTreePhase_PossibleConnectedness_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        generator.Settings.Random = new Random(112346);
        // generator.Settings.Shaper = NodeTreeGeneratorSettings.RadialShaper;
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
        foreach (RoomVertex<Vector2> vert in graph.Vertices)
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
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
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
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        var non_articulating_points = generator.GetTarjanNonArticulatingPoints(graph, dictionary);
        Assert.HasCount(25, non_articulating_points);
    }

    [TestMethod]
    public void NodeTreeGeneratorTarjans_2ArticulatingPoints_Valid()
    {
        var generator = new NodeTreeGenerator();
        var(graph, dictionary) = generator.GenerateFilledGraph(3, 3);
        var(graph2, dictionary2) = generator.GenerateFilledGraph(3, 3);
        graph.AddVerticesAndEdge(dictionary[new Vector2(1,1)].ConnectToVertex(dictionary2[new Vector2(1, 1)], Vector2.Zero));
        graph.AddVerticesAndEdgeRange(graph2.Edges);
        var non_articulating_points = generator.GetTarjanNonArticulatingPoints(graph, dictionary);
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
        backing_dictionary.TryAdd(center.Weight, center);
        for (int i = 2; i < 12; i++)
        {
            var new_vertex = new Vertex(new Vector2(i, i));
            graph.AddVerticesAndEdge(center.ConnectToVertex(new_vertex, Vector2.Zero));
            backing_dictionary.TryAdd(new_vertex.Weight, new_vertex);
        }
        var non_articulating_points = generator.GetTarjanNonArticulatingPoints(graph, backing_dictionary);
        Assert.HasCount(10, non_articulating_points);
    }

    // Currently deterministic
    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeHalfVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.Random = new Random(4881);
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 12);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeHalfVertices_Valid");
        Assert.HasCount(13, holes);
        Assert.HasCount(12, dictionary.Keys);
        Assert.HasCount(12, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeFifthVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 5);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeFifthVertices_Valid");
        Assert.HasCount(20, holes);
        Assert.HasCount(5, dictionary.Keys);
        Assert.HasCount(5, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSize1Vertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSize1Vertices_Valid");
        Assert.HasCount(24, holes);
        Assert.HasCount(1, dictionary.Keys);
        Assert.HasCount(1, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeNoVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 25);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeNoVertices_Valid");
        Assert.HasCount(0, holes);
        Assert.HasCount(25, dictionary.Keys);
        Assert.HasCount(25, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_TinySizeHalfVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(2, 2);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 2);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_TinySizeHalfVertices_Valid");
        Assert.HasCount(2, holes);
        Assert.HasCount(2, dictionary.Keys);
        Assert.HasCount(2, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_OneSizeNoVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(1, 1);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 0);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_OneSizeNoVertices_Valid");
        Assert.HasCount(1, holes);
        Assert.HasCount(0, dictionary.Keys);
        Assert.HasCount(0, graph.Vertices);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_OneSizeAllVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(1, 1);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_OneSizeAllVertices_Valid");
        Assert.HasCount(0, holes);
        Assert.HasCount(1, dictionary.Keys);
        Assert.HasCount(1, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeHalfVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        generator.Settings.WeightedVertexRemover = generator.Settings.AntiStrandingWeightedVertexRemover;
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1250);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeHalfVertices_Valid");
        Assert.HasCount(1250, holes);
        Assert.HasCount(1250, dictionary.Keys);
        Assert.HasCount(1250, graph.Vertices);
        CheckIsConnected(graph);
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
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 500);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeFifthVertices_Valid");
        Assert.HasCount(2000, holes);
        Assert.HasCount(500, dictionary.Keys);
        Assert.HasCount(500, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeAllButOneVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeAllButOneVertices_Valid");
        Assert.HasCount(2499, holes);
        Assert.HasCount(1, dictionary.Keys);
        Assert.HasCount(1, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeOnlyOneVertices_Valid()
    {
        var generator = new NodeTreeGenerator();
        generator.Settings.degree_percents = degree_weights;
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 2499);
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeOnlyOneVertices_Valid");
        Assert.HasCount(1, holes);
        Assert.HasCount(2499, dictionary.Keys);
        Assert.HasCount(2499, graph.Vertices);
        CheckIsConnected(graph);
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
            args.VertexFormat.Position = new QuikGraph.Graphviz.Dot.GraphvizPoint((int) args.Vertex.Weight.X * 72, (int) args.Vertex.Weight.Y * 72);
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

    // [Timeout(10000)]
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
        List<Vector2> directions = [Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY];
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