namespace TileMapGenerator;

using QuikGraph;
using QuikGraph.Algorithms;
using System.Numerics;
using System.Threading.Tasks;

[TestClass]
public class GraphBuilderTests
{
    [TestMethod]
    public void GraphBuilderTreePhase_PossibleConnectedness_Valid()
    {
        for (int min = 1; min < 7; min++)
        {
            for (int max = min; max < 7; max++)
            {
                UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>> output = new TileMapGenerator(5).GenerateNodeTree(1, [5], min, max, false);
                CheckVerticesDegrees(output, min, max);
                if (max != 0 && max != 1)
                {
                    UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>> output2 = new TileMapGenerator(5).GenerateNodeTree(1, [5], min, max, false, true );
                    CheckVerticesDegrees(output2, min, max);
                }
            }
        }
    }

    private void CheckVerticesDegrees(UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>> graph, int min, int max)
    {
        foreach (RoomVertex<Vector2> vert in graph.Vertices)
        {
            Assert.IsGreaterThanOrEqualTo(min, vert.Degree);
            Assert.IsLessThanOrEqualTo(max, vert.Degree);
        }
    }

    [TestMethod]
    public void GraphBuilderTreePhase_ImmpossibleConnectedness_Exception()
    {
        for (int i = 1; i < 7; i += 2)
        {
            Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], i, i, true, true ));
            Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], i, i, false));
        }
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], 0, 1, true, true ));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], 0, 0, true, true ));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], 1, 0, true, true ));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], 0, 1, false));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], 0, 0, false));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [5], 1, 0, false));
    }

    [TestMethod]
    public void GraphBuilderTreePhase_ConnectedUnit_Valid()
    {
        // all permutations of min and max 0 - 6, except min0-1+max=0-1
        for (int min = 0; min < 7; min++)
        {
            
            UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>> output = new TileMapGenerator(5).GenerateNodeTree(1, [5], min, (int) ((min+2)*1.25), false, true );
            CheckIsConnected(output);
            
        }
    }

    private void CheckIsConnected(UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>> graph)
    {
        var checker = new QuikGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<RoomVertex<Vector2>, RoomEdge<Vector2>>(graph);
        checker.Compute();
        Assert.AreEqual(1, checker.ComponentCount);
    }

    [TestMethod]
    public void GraphBuilderTreePhase_RoomNumber_Valid()
    {
        for (int r = 1; r <= 100; r++)
        {
            Assert.HasCount(r, new TileMapGenerator(5).GenerateNodeTree(1, [r], 3, 4, true, true ).Vertices);
            Assert.HasCount(r, new TileMapGenerator(5).GenerateNodeTree(1, [r], 3, 4, false).Vertices);
        }
        // creates valid number of rooms 1 - 100
    }

    [TestMethod]
    public void GraphBuilderTreePhase_RoomNumberWithDepth_Valid()
    {
        List<int> room_input = new List<int>();

        for (int r = 1; r <= 25; r+=5)
        {
            
            
            for (int i = 1; i <= 5; i++)
            {
                room_input.Clear();
                for (int j = 0; j < i; j++)
                {
                    room_input.Add(r);
                }
                Assert.HasCount(r*i, new TileMapGenerator(5).GenerateNodeTree(i, room_input, 3, 4, true, true ).Vertices);
                Assert.HasCount(r*i, new TileMapGenerator(5).GenerateNodeTree(i, room_input, 3, 4, false).Vertices);
            }
        }
        // creates valid number of rooms 1 - 25 (increment by five) at depths 1-5
    }

    [TestMethod]
    public void GraphBuilderTreePhase_RoomNumber_Invalid()
    {
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [0], 3, 4, true, true ));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [0], 3, 4, false));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [-1], 3, 4, true, true ));
        Assert.Throws<ArgumentException>(()=>new TileMapGenerator(5).GenerateNodeTree(1, [-1], 3, 4, false));
        // 0 rooms, negative rooms
    }

    [TestMethod]
    public void GraphBuilderTreePhase_LotsOfDepth_Valid()
    {
        List<int> room_input = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            room_input.Add(1);
        }
        var output = new TileMapGenerator(5).GenerateNodeTree(100, room_input, 3, 4, false, true );
        Assert.HasCount(1, output.Vertices);
        CheckIsConnected(output);

        output = new TileMapGenerator(5).GenerateNodeTree(100, room_input, 3, 4, true, true );
        Assert.HasCount(1, output.Vertices);
        CheckIsConnected(output);
    }

    [TestMethod]
    public void GraphBuilderTreePhase_NormalDepth_Valid()
    {
        List<int> room_input = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            room_input.Add(5);
        }
        for (int depth = 1; depth <= 10; depth++)
        {   
            room_input.Clear();
            for (int i = 0; i < depth; i++)
            {
                room_input.Add(5);
            }
            CheckIsConnected(new TileMapGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ));
            CheckIsConnected(new TileMapGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false));
            Assert.HasCount(depth*5, new TileMapGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, true, true ).Vertices);
            Assert.HasCount(depth*5, new TileMapGenerator(5).GenerateNodeTree(depth, room_input, 3, 4, false).Vertices);
        }
    }

    [TestMethod]
    public void GraphBuilderTreePhase_DepthVariableRoomNumber_Valid()
    {
        List<int> room_input = new List<int>();
        for (int rooms = 1; rooms <= 10; rooms++)
        {
            room_input.Clear();
            for (int i = 0; i < 10; i++)
            {
                room_input.Add(rooms);
            }
            var output = new TileMapGenerator(5).GenerateNodeTree(10, room_input, 3, 4, true, true );
            CheckIsConnected(output);
            Assert.HasCount(10*rooms, output.Vertices);
            CheckVerticesDegrees(output, 3, 4);

            output = new TileMapGenerator(5).GenerateNodeTree(10, room_input, 3, 4, false);
            CheckIsConnected(output);
            Assert.HasCount(10*rooms, output.Vertices);
            CheckVerticesDegrees(output, 3, 4);
        }
    }

    [TestMethod]
    public void GraphBuilderTreePhase_Depth_Invalid()
    {
        Assert.Throws<Exception>(()=>new TileMapGenerator(5).GenerateNodeTree(0, [5], 3, 4, true, true ));
        Assert.Throws<Exception>(()=>new TileMapGenerator(5).GenerateNodeTree(0, [5], 3, 4, false));
        Assert.Throws<Exception>(()=>new TileMapGenerator(5).GenerateNodeTree(-1, [5], 3, 4, true, true ));
        Assert.Throws<Exception>(()=>new TileMapGenerator(5).GenerateNodeTree(-1, [5], 3, 4, false));
    }

    [Timeout(1000)]
    [TestMethod]
    public void GraphBuilderTreePhase_SpeedConnected_Valid()
    {
        for (int i = 1; i <= 1000; i++)
        {
            new TileMapGenerator(5).GenerateNodeTree((i % 4)+1, [i, i, i, i], 3, 4, true, true );
        }
    }

    [Timeout(1000)]
    [TestMethod]
    public void GraphBuilderTreePhase_SpeedDisconnected_Valid()
    {
        for (int i = 1; i <= 1000; i++)
        {
            new TileMapGenerator(5).GenerateNodeTree((i % 4)+1, [i, i, i, i], 0, 1, false);
        }
    }


    [TestMethod]
    public void GraphBuilderTreePhase_WeightsCardinalDiagonal_Valid()
    {
        var vertices = new TileMapGenerator(5).GenerateNodeTree(1, [1], 1, 1).Vertices;
        foreach (var vertex in vertices)
        {
            var data = vertex.GetData();
        }
        // check various depths, room numbers, connectedness, max and mins
    }

    [TestMethod]
    public void GraphBuilderTreePhase_CentralMode_Valid()
    {
        // actually just make a copy of the other tests with this setting on
        // also test whether there is genuinely a central node
    }

    [TestMethod]
    public void GraphBuilderTreePhase_CheckUIDs_Valid()
    {
        
    }

    // check for overlap esp with depth generation
}