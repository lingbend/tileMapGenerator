namespace HallMaker
{
    using System.Numerics;
    using Graph = QuikGraph.UndirectedGraph<Primitives.ZVertex<System.Numerics.Vector2>, Primitives.ZEdge<System.Numerics.Vector2>>;
    using Vertex = Primitives.ZVertex<System.Numerics.Vector2>;
    using Edge = Primitives.ZEdge<System.Numerics.Vector2>;
    using System.Collections.Concurrent;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    // TODO: What's the status on this? Looks defunct
    public class HallMakerSettings
    {
    //     public List<Vector2> ValidDirections {get; set;}= new List<Vector2>()
    //     {Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY};
    //     public Random Random{get; set;} = new Random();

    //     public Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> Shaper{get; set;} = DefaultShaper;

    //     public int PruningSelectivityMultiplier{get; set;} = 1;

    //     public HallMakerSettings()
    //     {
    //     }
    
    //     public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> DefaultShaper{get;} = ((graph, backing, weight) =>
    //     {
    //     });

    //     public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> RadialShaper{get;} = ((graph, backing, weight) =>
    //     {
    //     });

    //     public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> CentralAxisXShaper{get;} = ((graph, backing, weight) =>
    //     {
    //     });

    //     public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> CentralAxisYShaper{get;} = ((graph, backing, weight) =>
    //     {
    //     });

    //     public Dictionary<int, int> degree_percents{get; set;} = DefaultDegreePercents;

    //     public static Dictionary<int, int> DefaultDegreePercents = new Dictionary<int, int>
    //     {
    //     };

    //     public Vector2 InitialRatio{get; set;} = Vector2.One;

    //     public int InitialPaddingPercent{get; set;} = 0;

    //     public Func<Vertex, Dictionary<int, int>, int> WeightedVertexRemover{get; set;}


    //     public int DefaultWeightedVertexRemover(Vertex vert, Dictionary<int, int> percents)
    //     {
    //     }

    //     public int AntiStrandingWeightedVertexRemover(Vertex vert, Dictionary<int, int> percents)
    //     {
    //     }

    //     public List<Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)>> PostProcessors{get; set;} = new List<Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)>>();

    //     public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)> HorizontalSymmetryPostProcessor{get;} = (graph, backing_dictionary) =>
    //     {
    //     };

    //     private static Vector2 GetHorizontalReflection(Vector2 weight, int median)
    //     {
    //     }

    //     private static Vector2 GetVerticalReflection(Vector2 weight, int median)
    //     {
    //     }


    //     public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)> VerticalSymmetryPostProcessor{get;} = (graph, backing_dictionary) =>
    //     {
    //     };

    

    


    }
}