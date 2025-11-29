namespace NodeTreeGenerator;

using TileMapGenerator;
using QuikGraph;
using QuikGraph.Algorithms;
using System.Numerics;
using System.Threading.Tasks;
using System.Diagnostics;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using QuikGraph.Graphviz;


[TestClass]
public class GraphBuilderTests
{
    Dictionary<int, int> degree_weights = new Dictionary<int, int>([KeyValuePair.Create(1, 1), KeyValuePair.Create(2, 10), KeyValuePair.Create(3, 15), KeyValuePair.Create(4, 15)]);

    [TestMethod]
    public void GraphBuilderTreePhase_PossibleConnectedness_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        // generator._random = new Random(112345);
        // generator._random = new Random(13513251);
        var (output, result) = generator.GenerateNodeTree([15], degree_weights);
        if (result)
        {
            CheckVerticesDegrees(output, 1, 4);
        }
        else
        {
            Debug.WriteLine("func returned false on exact");
        }
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

    [TestMethod]
    public void NodeTreeGeneratorGenerateFilledGraph_NormalSize_Valid()
    {
        var generator = new NodeTreeGenerator(5);
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
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        var non_articulating_points = generator.GetTarjanNonArticulatingPoints(graph, dictionary);
        Assert.HasCount(25, non_articulating_points);
    }

    [TestMethod]
    public void NodeTreeGeneratorTarjans_2ArticulatingPoints_Valid()
    {
        var generator = new NodeTreeGenerator(5);
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
        var generator = new NodeTreeGenerator(5);
        Graph graph = new Graph(false);
        Dictionary<Vector2, Vertex> backing_dictionary = new Dictionary<Vector2, Vertex>();
        var center = new Vertex(new Vector2(1,1));
        graph.AddVertex(center);
        backing_dictionary.Add(center.Weight, center);
        for (int i = 2; i < 12; i++)
        {
            var new_vertex = new Vertex(new Vector2(i, i));
            graph.AddVerticesAndEdge(center.ConnectToVertex(new_vertex, Vector2.Zero));
            backing_dictionary.Add(new_vertex.Weight, new_vertex);
        }
        var non_articulating_points = generator.GetTarjanNonArticulatingPoints(graph, backing_dictionary);
        Assert.HasCount(10, non_articulating_points);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeHalfVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 12, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeHalfVertices_Valid");
        Assert.HasCount(13, holes);
        Assert.HasCount(12, dictionary.Keys);
        Assert.HasCount(12, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeFifthVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 5, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeFifthVertices_Valid");
        Assert.HasCount(20, holes);
        Assert.HasCount(5, dictionary.Keys);
        Assert.HasCount(5, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSize1Vertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSize1Vertices_Valid");
        Assert.HasCount(24, holes);
        Assert.HasCount(1, dictionary.Keys);
        Assert.HasCount(1, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_NormalSizeNoVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(5, 5);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 25, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_NormalSizeNoVertices_Valid");
        Assert.HasCount(0, holes);
        Assert.HasCount(25, dictionary.Keys);
        Assert.HasCount(25, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_TinySizeHalfVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(2, 2);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 2, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_TinySizeHalfVertices_Valid");
        Assert.HasCount(2, holes);
        Assert.HasCount(2, dictionary.Keys);
        Assert.HasCount(2, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_OneSizeNoVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(1, 1);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 0, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_OneSizeNoVertices_Valid");
        Assert.HasCount(1, holes);
        Assert.HasCount(0, dictionary.Keys);
        Assert.HasCount(0, graph.Vertices);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_OneSizeAllVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(1, 1);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_OneSizeAllVertices_Valid");
        Assert.HasCount(0, holes);
        Assert.HasCount(1, dictionary.Keys);
        Assert.HasCount(1, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeHalfVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1250, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeHalfVertices_Valid");
        Assert.HasCount(1250, holes);
        Assert.HasCount(1250, dictionary.Keys);
        Assert.HasCount(1250, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeFifthVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 500, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeFifthVertices_Valid");
        Assert.HasCount(2000, holes);
        Assert.HasCount(500, dictionary.Keys);
        Assert.HasCount(500, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeAllButOneVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 1, new Vector2(5, 5));
        PrintToPNG(graph, "NodeTreeGeneratorCutVerticesDownTo_HugeSizeAllButOneVertices_Valid");
        Assert.HasCount(2499, holes);
        Assert.HasCount(1, dictionary.Keys);
        Assert.HasCount(1, graph.Vertices);
        CheckIsConnected(graph);
    }

    [TestMethod]
    public void NodeTreeGeneratorCutVerticesDownTo_HugeSizeOnlyOneVertices_Valid()
    {
        var generator = new NodeTreeGenerator(5);
        var(graph, dictionary) = generator.GenerateFilledGraph(50, 50);
        (graph, dictionary, var holes) = generator.CutVerticesDownTo(graph, dictionary, 2499, new Vector2(5, 5));
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
        string file = new GraphvizAlgorithm<Vertex, Edge>(graph).Generate();
        File.WriteAllText($"../../{name}.dot", file);
        using var process = Process.Start("dot", $"-Tpng ../../{name}.dot -o ../../{name}.png");
        process.WaitForExit();
    }


    // [TestMethod]
    // public void GraphBuilderTreePhase_ImmpossibleConnectedness_Exception()
    // {
    //     for (int i = 1; i < 7; i += 2)
    //     {
    //         Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [5], i, i, true, true ));
    //         Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree([5], degree_weights));
    //     }
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [5], 0, 1, true, true ));
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [5], 0, 0, true, true ));
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [5], 1, 0, true, true ));
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [5], 0, 1, false));
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [5], 0, 0, false));
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [5], 1, 0, false));
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_ConnectedUnit_Valid()
    // {
    //     // all permutations of min and max 0 - 6, except min0-1+max=0-1
    //     for (int min = 0; min < 7; min++)
    //     {
            
    //         UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>> output = new NodeTreeGenerator(5).GenerateNodeTree(1, [5], min, (int) ((min+2)*1.25), false, true );
    //         CheckIsConnected(output);
            
    //     }
    // }



    // [TestMethod]
    // public void GraphBuilderTreePhase_RoomNumber_Valid()
    // {
    //     for (int r = 1; r <= 100; r++)
    //     {
    //         Assert.HasCount(r, new NodeTreeGenerator(5).GenerateNodeTree(1, [r], 3, 4, true, true ).Vertices);
    //         Assert.HasCount(r, new NodeTreeGenerator(5).GenerateNodeTree(1, [r], 3, 4, false).Vertices);
    //     }
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_RoomNumberWithDepthDecentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int r = 10;
    //     int i = 3;   

    //     room_input.Clear();
    //     for (int j = 0; j < i; j++)
    //     {
    //         room_input.Add(r);
    //     }
    //     Assert.HasCount((int) Math.Pow(r, i), new NodeTreeGenerator(5).GenerateNodeTree(i, room_input, 3, 4, false).Vertices);

    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_RoomNumberWithDepthCentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int r = 10;
    //     int i = 3;   

    //     room_input.Clear();
    //     for (int j = 0; j < i; j++)
    //     {
    //         room_input.Add(r);
    //     }
    //     Assert.HasCount((int) Math.Pow(r, i), new NodeTreeGenerator(5).GenerateNodeTree(i, room_input, 3, 4, true, true ).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_RoomNumber0Decentral_Invalid()
    // {
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [0], 3, 4, false));
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_RoomNumber0Central_Invalid()
    // {
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [0], 3, 4, true, true ));
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_RoomNumberNegativeDecentral_Invalid()
    // {
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [-1], 3, 4, false));
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_RoomNumberNegativeCentral_Invalid()
    // {
    //     Assert.Throws<ArgumentException>(()=>new NodeTreeGenerator(5).GenerateNodeTree(1, [-1], 3, 4, true, true ));
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_LotsOfDepthDecentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     for (int i = 0; i < 100; i++)
    //     {
    //         room_input.Add(1);
    //     }
    //     var output = new NodeTreeGenerator(5).GenerateNodeTree(100, room_input, 3, 4, false, true );
    //     Assert.HasCount(1, output.Vertices);
    //     CheckIsConnected(output);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_LotsOfDepthCentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     for (int i = 0; i < 100; i++)
    //     {
    //         room_input.Add(1);
    //     }
    //     var output = new NodeTreeGenerator(5).GenerateNodeTree(100, room_input, 3, 4, true, true );
    //     Assert.HasCount(1, output.Vertices);
    //     CheckIsConnected(output);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_1DepthDecentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 1;
       
    //     room_input.Clear();
    //     for (int i = 0; i < depth; i++)
    //     {
    //         room_input.Add(5);
    //     }
    //     CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false));
    //     Assert.HasCount((int) Math.Pow(5, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhaseCentral_1Depth_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 1;
       
    //     room_input.Clear();
    //     for (int i = 0; i < depth; i++)
    //     {
    //         room_input.Add(5);
    //     }
    //     CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ));
    //     Assert.HasCount((int) Math.Pow(5, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_3DepthDecentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 3;

    //     room_input.Clear();
    //     for (int i = 0; i < depth; i++)
    //     {
    //         room_input.Add(5);
    //     }
    //     CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false));
    //     Assert.HasCount((int) Math.Pow(5, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_3DepthCentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 3;

    //     room_input.Clear();
    //     for (int i = 0; i < depth; i++)
    //     {
    //         room_input.Add(5);
    //     }
    //     CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ));
    //     Assert.HasCount((int) Math.Pow(5, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_4DepthDecentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 4; 
    //         room_input.Clear();
    //         for (int i = 0; i < depth; i++)
    //         {
    //             room_input.Add(5);
    //         }
    //         CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false));
    //         Assert.HasCount((int) Math.Pow(5, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_4DepthCentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 4; 
    //         room_input.Clear();
    //         for (int i = 0; i < depth; i++)
    //         {
    //             room_input.Add(5);
    //         }
    //         CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ));
    //         Assert.HasCount((int) Math.Pow(5, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_7DepthDecentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 7;
  
    //     room_input.Clear();
    //     for (int i = 0; i < depth; i++)
    //     {
    //         room_input.Add(3);
    //     }
    //     CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false));
    //     Assert.HasCount((int) Math.Pow(3, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_7DepthCentral_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     int depth = 7;
  
    //     room_input.Clear();
    //     for (int i = 0; i < depth; i++)
    //     {
    //         room_input.Add(3);
    //     }
    //     CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ));
    //     Assert.HasCount((int) Math.Pow(3, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ).Vertices);
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_NormalDepthStressTest_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     for (int depth = 1; depth <= 7; depth++)
    //     {   
    //         room_input.Clear();
    //         for (int i = 0; i < depth; i++)
    //         {
    //             room_input.Add(3);
    //         }
    //         CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ));
    //         CheckIsConnected(new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false));
    //         Assert.HasCount((int) Math.Pow(3, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ).Vertices);
    //         Assert.HasCount((int) Math.Pow(3, depth), new NodeTreeGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false).Vertices);
    //     }
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_DepthVariableRoomNumber_Valid()
    // {
    //     List<int> room_input = new List<int>();
    //     for (int rooms = 1; rooms <= 10; rooms++)
    //     {
    //         room_input.Clear();
    //         for (int i = 0; i < 10; i++)
    //         {
    //             room_input.Add(rooms);
    //         }
    //         var output = new NodeTreeGenerator(5).GenerateNodeTree(10, room_input, 3, 4, true, true );
    //         CheckIsConnected(output);
    //         Assert.HasCount((int) Math.Pow(rooms, 10), output.Vertices);
    //         if (rooms != 1)
    //         {
    //             CheckVerticesDegrees(output, 3, 4);
    //         }
    //         else
    //         {
    //             CheckVerticesDegrees(output, 0, 0);
    //         }
            

    //         output = new NodeTreeGenerator(5).GenerateNodeTree(10, room_input, 3, 4, false);
    //         CheckIsConnected(output);
    //         Assert.HasCount((int) Math.Pow(rooms, 10), output.Vertices);
    //         CheckVerticesDegrees(output, 3, 4);
    //         if (rooms != 1)
    //         {
    //             CheckVerticesDegrees(output, 3, 4);
    //         }
    //         else
    //         {
    //             CheckVerticesDegrees(output, 0, 0);
    //         }
            
    //     }
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_Depth_Invalid()
    // {
    //     Assert.Throws<Exception>(()=>new NodeTreeGenerator(5).GenerateNodeTree(0, [5], 3, 4, true, true ));
    //     Assert.Throws<Exception>(()=>new NodeTreeGenerator(5).GenerateNodeTree(0, [5], 3, 4, false));
    //     Assert.Throws<Exception>(()=>new NodeTreeGenerator(5).GenerateNodeTree(-1, [5], 3, 4, true, true ));
    //     Assert.Throws<Exception>(()=>new NodeTreeGenerator(5).GenerateNodeTree(-1, [5], 3, 4, false));
    // }

    // [Timeout(1000)]
    // [TestMethod]
    // public void GraphBuilderTreePhase_SpeedConnected_Valid()
    // {
    //     for (int i = 1; i <= 1000; i++)
    //     {
    //         new NodeTreeGenerator(5).GenerateNodeTree((i % 4)+1, [i, i, i, i], 3, 4, true, true );
    //     }
    // }

    // [Timeout(1000)]
    // [TestMethod]
    // public void GraphBuilderTreePhase_SpeedDisconnected_Valid()
    // {
    //     for (int i = 1; i <= 1000; i++)
    //     {
    //         new NodeTreeGenerator(5).GenerateNodeTree((i % 4)+1, [i, i, i, i], 0, 1, false, false);
    //     }
    // }


    // [TestMethod]
    // public void GraphBuilderTreePhase_WeightsCardinalDiagonal_Valid()
    // {
    //     // var vertices = new NodeTreeGenerator(5).GenerateNodeTree(1, [1], 1, 1).Vertices;
    //     // foreach (var vertex in vertices)
    //     // {
    //     //     var data = vertex.GetData();
    //     // }
    //     // check various depths, room numbers, connectedness, max and mins
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_CentralMode_Valid()
    // {
    //     // actually just make a copy of the other tests with this setting on
    //     // also test whether there is genuinely a central node
    // }

    // [TestMethod]
    // public void GraphBuilderTreePhase_CheckUIDs_Valid()
    // {
        
    // }

    // // check for overlap esp with depth generation

    // [TestMethod]
    // public void UnpackGraphWeights_Count_Valid()
    // {
        
    // }
}