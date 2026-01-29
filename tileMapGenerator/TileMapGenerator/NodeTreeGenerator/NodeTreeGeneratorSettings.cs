namespace NodeTreeGenerator;
using System.Numerics;
using MapPrimatives;
using Graph = QuikGraph.UndirectedGraph<MapPrimatives.RoomVertex<System.Numerics.Vector2>, MapPrimatives.RoomEdge<System.Numerics.Vector2>>;
using Vertex = MapPrimatives.RoomVertex<System.Numerics.Vector2>;
using Edge = MapPrimatives.RoomEdge<System.Numerics.Vector2>;
using System.Collections.Concurrent;


public class NodeTreeGeneratorSettings
{
    public List<Vector2> ValidDirections {get; set;}= [Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX,
     -Vector2.UnitY];
    public Random Random{get; set;} = new Random();

    public Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> Shaper{get; set;} = DefaultShaper;

    public int PruningSelectivityMultiplier{get; set;} = 1;

    public NodeTreeGeneratorSettings()
    {
        WeightedVertexRemover = DefaultWeightedVertexRemover;
    }
    
    public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> DefaultShaper{get;} = ((graph, backing, weight) =>
    {
        int size = graph.VertexCount;
        Dictionary<int, int> enum_numbering = new Dictionary<int, int>();
        List<int> nums = new List<int>();
        foreach (var degree in weight.Keys.OrderDescending())
        {
            enum_numbering.Add(degree,(int) (weight[degree]*.01 * size));
        }
        int size_error = size - enum_numbering.Values.Sum();
        foreach(int degree in enum_numbering.Keys.OrderDescending())
        {
            for(int i = 0; i < enum_numbering[degree]; i++)
            {
                nums.Add(degree);
            }
        }
        for (int i = 0; i < size_error; i++)
        {
            nums.Add(weight.Keys.First());
        }
        return backing.Values.OrderBy(v=>v.ID).Zip(nums);
    });

    public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> RadialShaper{get;} = ((graph, backing, weight) =>
    {
        int size = graph.VertexCount;
        Dictionary<int, int> enum_numbering = new Dictionary<int, int>();
        List<int> nums = new List<int>();
        foreach (var degree in weight.Keys.OrderDescending())
        {
            enum_numbering.Add(degree,(int) (weight[degree]*.01 * size));
        }
        int size_error = size - enum_numbering.Values.Sum();
        foreach(int degree in enum_numbering.Keys.OrderDescending())
        {
            for(int i = 0; i < enum_numbering[degree]; i++)
            {
                nums.Add(degree);
            }
        }
        for (int i = 0; i < size_error; i++)
        {
            nums.Add(weight.Keys.First());
        }

        int min_size_x = (int) backing.Keys.MinBy((v)=>v.X).X;
        int min_size_y = (int) backing.Keys.MinBy((v)=>v.Y).Y;
        int max_size_x = (int) backing.Keys.MaxBy((v)=>v.X).X;
        int max_size_y = (int) backing.Keys.MaxBy((v)=>v.Y).Y;
        Vector2 middle = new Vector2((max_size_x + min_size_x)/2, (max_size_y+min_size_y)/2);
        List<(int, Vertex)> vertices = new List<(int, Vertex)>();
        foreach (Vertex vertex in backing.Values.OrderBy(v=>v.ID))
        {
            vertices.Add(((int) Math.Abs((vertex.Weight - middle).Length()), vertex));
        }
        vertices = new List<(int, Vertex)>(vertices.OrderBy((t)=>t.Item1));

        List<Vertex> cleaned_vertices = new List<Vertex>();
        foreach (var (length, vert) in vertices)
        {
            cleaned_vertices.Add(vert);
        }

        return cleaned_vertices.Zip(nums);
    });

    public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> CentralAxisXShaper{get;} = ((graph, backing, weight) =>
    {
        int size = graph.VertexCount;
        Dictionary<int, int> enum_numbering = new Dictionary<int, int>();
        List<int> nums = new List<int>();
        foreach (var degree in weight.Keys.OrderDescending())
        {
            enum_numbering.Add(degree,(int) (weight[degree]*.01 * size));
        }
        int size_error = size - enum_numbering.Values.Sum();
        foreach(int degree in enum_numbering.Keys.Order())
        {
            for(int i = 0; i < enum_numbering[degree]; i++)
            {
                nums.Add(degree);
            }
        }
        for (int i = 0; i < size_error; i++)
        {
            nums.Add(weight.Keys.First());
        }

        int min_size_x = (int) backing.Keys.MinBy((v)=>v.X).X;
        int min_size_y = (int) backing.Keys.MinBy((v)=>v.Y).Y;
        int max_size_x = (int) backing.Keys.MaxBy((v)=>v.X).X;
        int max_size_y = (int) backing.Keys.MaxBy((v)=>v.Y).Y;
        Vector2 middle = new Vector2((max_size_x + min_size_x)/2, (max_size_y+min_size_y)/2);
        List<(int, Vertex)> vertices = new List<(int, Vertex)>();
        foreach (Vertex vertex in backing.Values.OrderBy(v=>v.ID))
        {
            vertices.Add(((int) Math.Abs(vertex.Weight.X - middle.X), vertex));
        }
        vertices = new List<(int, Vertex)>(vertices.OrderBy((t)=>t.Item1));

        List<Vertex> cleaned_vertices = new List<Vertex>();
        foreach (var (length, vert) in vertices)
        {
            cleaned_vertices.Add(vert);
        }

        return cleaned_vertices.Zip(nums);
    });

    public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> CentralAxisYShaper{get;} = ((graph, backing, weight) =>
    {
        int size = graph.VertexCount;
        Dictionary<int, int> enum_numbering = new Dictionary<int, int>();
        List<int> nums = new List<int>();
        foreach (var degree in weight.Keys.OrderDescending())
        {
            enum_numbering.Add(degree,(int) (weight[degree]*.01 * size));
        }
        int size_error = size - enum_numbering.Values.Sum();
        foreach(int degree in enum_numbering.Keys.OrderDescending())
        {
            for(int i = 0; i < enum_numbering[degree]; i++)
            {
                nums.Add(degree);
            }
        }
        for (int i = 0; i < size_error; i++)
        {
            nums.Add(weight.Keys.Order().First());
        }

        int min_size_x = (int) backing.Keys.MinBy((v)=>v.X).X;
        int min_size_y = (int) backing.Keys.MinBy((v)=>v.Y).Y;
        int max_size_x = (int) backing.Keys.MaxBy((v)=>v.X).X;
        int max_size_y = (int) backing.Keys.MaxBy((v)=>v.Y).Y;
        Vector2 middle = new Vector2((max_size_x + min_size_x)/2, (max_size_y+min_size_y)/2);
        List<(int, Vertex)> vertices = new List<(int, Vertex)>();
        foreach (Vertex vertex in backing.Values.OrderBy(v=>v.ID))
        {
            vertices.Add(((int) Math.Abs(vertex.Weight.Y - middle.Y), vertex));
        }
        vertices = new List<(int, Vertex)>(vertices.OrderBy((t)=>t.Item1));

        List<Vertex> cleaned_vertices = new List<Vertex>();
        foreach (var (length, vert) in vertices)
        {
            cleaned_vertices.Add(vert);
        }

        return cleaned_vertices.Zip(nums);
    });

    public Dictionary<int, int> degree_percents{get; set;} = new Dictionary<int, int>();

    public Vector2 InitialRatio{get; set;} = Vector2.One;

    public int InitialPaddingPercent{get; set;} = 0;

    public Func<Vertex, Dictionary<int, int>, int> WeightedVertexRemover{get; set;}


    public int DefaultWeightedVertexRemover(Vertex vert, Dictionary<int, int> percents)
    {
        int distance = 0;
        for(int i = 1; i < percents.Count; i++)
        {
            int temp_distance = Math.Abs(percents.Values.ElementAt(i-1) - percents.Values.ElementAt(i));
            if (temp_distance > distance)
            {
                distance = (int) Math.Ceiling(Math.Pow(temp_distance, 1.1));
            }
        }
        int jitter = Random.Next(-distance,distance+1);
        if (percents.TryGetValue(vert.Degree, out int val))
        {
            return 100 - val + jitter;
        }
        else
        {
            return 100 + jitter;
        }
    }

    public int AntiStrandingWeightedVertexRemover(Vertex vert, Dictionary<int, int> percents)
    {
        int distance = 0;
        for(int i = 1; i < percents.Count; i++)
        {
            int temp_distance = Math.Abs(percents.Values.ElementAt(i-1) - percents.Values.ElementAt(i));
            if (temp_distance > distance)
            {
                distance = (int) Math.Ceiling(Math.Pow(temp_distance, 1.1));
            }
        }
        int jitter = Random.Next(-distance,distance+1);
        if (vert.Degree == 1 || vert.Degree == 2)
        {
            List<int> neighboring_degrees = new List<int>(vert.Degree);
            foreach (Edge edge in vert.Edges.OrderBy(e=>e.ID))
            {
                if (vert == edge.Source)
                {
                    neighboring_degrees.Add(edge.Target.Degree);
                }
                else
                {
                    neighboring_degrees.Add(edge.Source.Degree);
                }                
            }
            if (neighboring_degrees.Sum() / (double) neighboring_degrees.Count <= 2)
            {
                if (percents.TryGetValue(vert.Degree, out int val2))
                {
                    return (100 - val2+jitter)*3;
                }
                else
                {
                    return 300+jitter;
                }
            }
        }
        
        if (percents.TryGetValue(vert.Degree, out int val))
        {
            return 100 - val + jitter;
        }
        else
        {
            return 100 + jitter;
        }
    }

    public List<Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)>> PostProcessors{get; set;} = new List<Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)>>();

    public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)> HorizontalSymmetryPostProcessor{get;} = (graph, backing_dictionary) =>
    {
        List<Vertex> weights = new List<Vertex>(graph.Vertices.OrderBy((vert)=>vert.Weight.Y));
        List<Vertex> median_list;
        int median;
        Vertex? extra = null;

        Graph new_graph = new Graph(false);
        ConcurrentDictionary<Vector2, Vertex> new_backing_dictionary = new ConcurrentDictionary<Vector2, Vertex>();

        if (weights.Count % 2 == 0)
        {
            median_list = weights[..(weights.Count / 2)];
            median = (int) median_list[^1].Weight.Y + 1;
        }
        else
        {
            median_list = weights[..((weights.Count-1) / 2)];
            extra = weights[(weights.Count-1)/2];
            var extra_vertex = new Vertex(extra.Weight);
            new_backing_dictionary.TryAdd(extra.Weight, extra_vertex);
            new_graph.AddVertex(extra_vertex);
            median = (int) extra.Weight.Y + 1;
        }
        
        foreach (Vertex vertex in median_list)
        {
            Vertex new_vertex = new Vertex(vertex.Weight);
            Vertex reflected_vertex = new Vertex(GetHorizontalReflection(new_vertex.Weight, median));
            new_graph.AddVertex(new_vertex);
            new_graph.AddVertex(reflected_vertex);
            new_backing_dictionary.TryAdd(new_vertex.Weight, new_vertex);
            new_backing_dictionary.TryAdd(reflected_vertex.Weight, reflected_vertex);
        }
        if (extra != null)
        {
            median_list.Add(extra);
        }
        List<(Vertex, Vertex)> possible_connection_pairs = new List<(Vertex, Vertex)>();
        foreach (Vertex vertex in median_list)
        {
            foreach (Edge edge in vertex.Edges.OrderBy(e=>e.ID))
            {
                if (extra != null && vertex.Weight == extra?.Weight)
                {
                    Vertex other_vertex;
                    
                    if (extra.Weight == edge.Source.Weight)
                    {
                        other_vertex = edge.Target;
                    }
                    else
                    {
                        other_vertex = edge.Source;
                    }

                    if (!new_backing_dictionary.ContainsKey(other_vertex.Weight))
                    {
                        continue;
                    }

                    Vector2 other_vertex_weight = other_vertex.Weight;
       
                    if (!new_graph.ContainsVertex(new_backing_dictionary[extra.Weight]))
                    {
                        throw new Exception();
                    }
                    else if (!new_graph.ContainsVertex(new_backing_dictionary[other_vertex_weight]))
                    {
                        throw new Exception();
                    }

                    new_graph.AddEdge(new_backing_dictionary[extra.Weight]
                    .ConnectToVertex(new_backing_dictionary[other_vertex_weight], extra.Weight - other_vertex_weight));
                    new_graph.AddEdge(new_backing_dictionary[extra.Weight].ConnectToVertex(new_backing_dictionary
                    [GetHorizontalReflection(other_vertex_weight, median)], (extra.Weight - other_vertex_weight)*new Vector2(0, -1)));
                }
                else if(median_list.Contains(edge.Source) != median_list.Contains(edge.Target) && edge.Target != extra && edge.Source != extra)
                {
                    Vertex insider = median_list.Contains(edge.Source) ? edge.Source : edge.Target;
                    new_graph.AddEdge(new_backing_dictionary[insider.Weight].ConnectToVertex(new_backing_dictionary
                    [GetHorizontalReflection(insider.Weight, median)], edge.Weight));
                    possible_connection_pairs.Add((insider, new_backing_dictionary
                    [GetHorizontalReflection(insider.Weight, median)]));
                }
                else if (edge.Target != extra && edge.Source != extra)
                {
                    new_graph.AddEdge(new_backing_dictionary[edge.Source.Weight]
                    .ConnectToVertex(new_backing_dictionary[edge.Target.Weight], edge.Weight));

                    new_graph.AddEdge(new_backing_dictionary[GetHorizontalReflection(edge.Source.Weight, median)]
                    .ConnectToVertex(new_backing_dictionary[GetHorizontalReflection(edge.Target.Weight, median)],
                    edge.Weight*new Vector2(0, -1)));
                }
            }
        }
        List<Graph> connected_components = new(NodeTreeGenerator.GetConnectedComponents(new_graph).OrderBy(g=>g.EdgeCount * g.VertexCount));
        int counter = connected_components.Count*3;
        Graph unified_graph = new Graph(false);
        while (connected_components.Count > 1 && counter > 0)
        {
            
            foreach (Graph subgraph in connected_components[1..])
            {
                List<(Vertex, Vertex)> pairs_in_both = new List<(Vertex, Vertex)>();
                foreach(var (vertex1, vertex2) in possible_connection_pairs)
                {
                    if ((unified_graph.ContainsVertex(vertex1) && subgraph.ContainsVertex(vertex2)) ||
                        (unified_graph.ContainsVertex(vertex2) && subgraph.ContainsVertex(vertex1)))
                    {
                        pairs_in_both.Add((vertex1, vertex2));
                    }
                }
                if (pairs_in_both.Count > 1)
                {
                    pairs_in_both = new List<(Vertex, Vertex)>(pairs_in_both.OrderBy((tuple) =>
                    {
                        return Math.Abs((tuple.Item1.Weight - tuple.Item2.Weight).Length());
                    }));
                    new_graph.AddVerticesAndEdge(pairs_in_both[0].Item1.ConnectToVertex(pairs_in_both[0].Item2, pairs_in_both[0].Item2.Weight - pairs_in_both[0].Item1.Weight));
                    new_graph.AddVerticesAndEdgeRange(subgraph.Edges);
                }
                else if (connected_components.Count+1 > counter)
                {
                    List<(Vertex, Vertex)> alternative_pairs = new List<(Vertex, Vertex)>();
                    bool found = false;
                    foreach(Vertex vertex3 in new_graph.Vertices)
                    {
                        foreach(Vertex vertex4 in subgraph.Vertices)
                        {
                            if (vertex3.Weight != vertex4.Weight)
                            {
                                alternative_pairs.Add((vertex3, vertex4));
                            }
                            if (Math.Abs((vertex3.Weight - vertex4.Weight).Length()) <= 1)
                            {

                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                    alternative_pairs = new List<(Vertex, Vertex)>(alternative_pairs.OrderBy((tuple) =>
                    {
                        return Math.Abs((tuple.Item1.Weight - tuple.Item2.Weight).Length());
                    }));
                    new_graph.AddVerticesAndEdge(alternative_pairs[0].Item1.ConnectToVertex(alternative_pairs[0].Item2, alternative_pairs[0].Item2.Weight - alternative_pairs[0].Item1.Weight));
                    new_graph.AddVerticesAndEdgeRange(subgraph.Edges);
                }
            }
            connected_components = NodeTreeGenerator.GetConnectedComponents(new_graph);
            counter--;
        }
        return (new_graph, new_backing_dictionary);
    };

    private static Vector2 GetHorizontalReflection(Vector2 weight, int median)
    {
        return new Vector2(weight.X, median)+new Vector2(0, median - weight.Y);
    }

    private static Vector2 GetVerticalReflection(Vector2 weight, int median)
    {
        return new Vector2(median, weight.Y)+new Vector2(median - weight.X, 0);
    }


    public static Func<Graph, ConcurrentDictionary<Vector2, Vertex>, (Graph, ConcurrentDictionary<Vector2, Vertex>)> VerticalSymmetryPostProcessor{get;} = (graph, backing_dictionary) =>
    {
        List<Vertex> weights = new List<Vertex>(graph.Vertices.OrderBy((vert)=>vert.Weight.X));
        List<Vertex> median_list;
        int median;
        Vertex? extra = null;

        Graph new_graph = new Graph(false);
        ConcurrentDictionary<Vector2, Vertex> new_backing_dictionary = new ConcurrentDictionary<Vector2, Vertex>();

        if (weights.Count % 2 == 0)
        {
            median_list = weights[..(weights.Count / 2)];
            median = (int) median_list[^1].Weight.X + 1;
        }
        else
        {
            median_list = weights[..((weights.Count-1) / 2)];
            extra = weights[(weights.Count-1)/2];
            var extra_vertex = new Vertex(extra.Weight);
            new_backing_dictionary.TryAdd(extra.Weight, extra_vertex);
            new_graph.AddVertex(extra_vertex);
            median = (int) extra.Weight.X + 1;
        }
        
        foreach (Vertex vertex in median_list)
        {
            Vertex new_vertex = new Vertex(vertex.Weight);
            Vertex reflected_vertex = new Vertex(GetVerticalReflection(new_vertex.Weight, median));
            new_graph.AddVertex(new_vertex);
            new_graph.AddVertex(reflected_vertex);
            new_backing_dictionary.TryAdd(new_vertex.Weight, new_vertex);
            new_backing_dictionary.TryAdd(reflected_vertex.Weight, reflected_vertex);
        }
        if (extra != null)
        {
            median_list.Add(extra);
        }
        List<(Vertex, Vertex)> possible_connection_pairs = new List<(Vertex, Vertex)>();
        foreach (Vertex vertex in median_list)
        {
            foreach (Edge edge in vertex.Edges.OrderBy(e=>e.ID))
            {
                if (extra != null && vertex.Weight == extra?.Weight)
                {
                    Vertex other_vertex;
                    
                    if (extra.Weight == edge.Source.Weight)
                    {
                        other_vertex = edge.Target;
                    }
                    else
                    {
                        other_vertex = edge.Source;
                    }

                    if (!new_backing_dictionary.ContainsKey(other_vertex.Weight))
                    {
                        continue;
                    }

                    Vector2 other_vertex_weight = other_vertex.Weight;
       
                    if (!new_graph.ContainsVertex(new_backing_dictionary[extra.Weight]))
                    {
                        throw new Exception();
                    }
                    else if (!new_graph.ContainsVertex(new_backing_dictionary[other_vertex_weight]))
                    {
                        throw new Exception();
                    }

                    new_graph.AddEdge(new_backing_dictionary[extra.Weight]
                    .ConnectToVertex(new_backing_dictionary[other_vertex_weight], extra.Weight - other_vertex_weight));
                    new_graph.AddEdge(new_backing_dictionary[extra.Weight].ConnectToVertex(new_backing_dictionary
                    [GetVerticalReflection(other_vertex_weight, median)], (extra.Weight - other_vertex_weight)*new Vector2(-1, 0)));
                }
                else if(median_list.Contains(edge.Source) != median_list.Contains(edge.Target) && edge.Target != extra && edge.Source != extra)
                {
                    Vertex insider = median_list.Contains(edge.Source) ? edge.Source : edge.Target;
                    new_graph.AddEdge(new_backing_dictionary[insider.Weight].ConnectToVertex(new_backing_dictionary
                    [GetVerticalReflection(insider.Weight, median)], edge.Weight));
                    possible_connection_pairs.Add((insider, new_backing_dictionary
                    [GetVerticalReflection(insider.Weight, median)]));
                }
                else if (edge.Target != extra && edge.Source != extra)
                {
                    new_graph.AddEdge(new_backing_dictionary[edge.Source.Weight]
                    .ConnectToVertex(new_backing_dictionary[edge.Target.Weight], edge.Weight));

                    new_graph.AddEdge(new_backing_dictionary[GetVerticalReflection(edge.Source.Weight, median)]
                    .ConnectToVertex(new_backing_dictionary[GetVerticalReflection(edge.Target.Weight, median)],
                    edge.Weight*new Vector2(-1, 0)));
                }
            }
        }
        List<Graph> connected_components = new (NodeTreeGenerator.GetConnectedComponents(new_graph).OrderBy(g=>g.EdgeCount * g.VertexCount));
        int counter = connected_components.Count*3;
        Graph unified_graph = new Graph(false);
        while (connected_components.Count > 1 && counter > 0)
        {
            
            foreach (Graph subgraph in connected_components[1..])
            {
                List<(Vertex, Vertex)> pairs_in_both = new List<(Vertex, Vertex)>();
                foreach(var (vertex1, vertex2) in possible_connection_pairs)
                {
                    if ((unified_graph.ContainsVertex(vertex1) && subgraph.ContainsVertex(vertex2)) ||
                        (unified_graph.ContainsVertex(vertex2) && subgraph.ContainsVertex(vertex1)))
                    {
                        pairs_in_both.Add((vertex1, vertex2));
                    }
                }
                if (pairs_in_both.Count > 1)
                {
                    pairs_in_both = new List<(Vertex, Vertex)>(pairs_in_both.OrderBy((tuple) =>
                    {
                        return Math.Abs((tuple.Item1.Weight - tuple.Item2.Weight).Length());
                    }));
                    new_graph.AddVerticesAndEdge(pairs_in_both[0].Item1.ConnectToVertex(pairs_in_both[0].Item2, pairs_in_both[0].Item2.Weight - pairs_in_both[0].Item1.Weight));
                    new_graph.AddVerticesAndEdgeRange(subgraph.Edges);
                }
                else if (connected_components.Count+1 > counter)
                {
                    List<(Vertex, Vertex)> alternative_pairs = new List<(Vertex, Vertex)>();
                    bool found = false;
                    foreach(Vertex vertex3 in new_graph.Vertices)
                    {
                        foreach(Vertex vertex4 in subgraph.Vertices)
                        {
                            if (vertex3.Weight != vertex4.Weight)
                            {
                                alternative_pairs.Add((vertex3, vertex4));
                            }
                            if (Math.Abs((vertex3.Weight - vertex4.Weight).Length()) <= 1)
                            {

                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                    alternative_pairs = new List<(Vertex, Vertex)>(alternative_pairs.OrderBy((tuple) =>
                    {
                        return Math.Abs((tuple.Item1.Weight - tuple.Item2.Weight).Length());
                    }));
                    new_graph.AddVerticesAndEdge(alternative_pairs[0].Item1.ConnectToVertex(alternative_pairs[0].Item2, alternative_pairs[0].Item2.Weight - alternative_pairs[0].Item1.Weight));
                    new_graph.AddVerticesAndEdgeRange(subgraph.Edges);
                }
            }
            connected_components = NodeTreeGenerator.GetConnectedComponents(new_graph);
            counter--;
        }
        return (new_graph, new_backing_dictionary);
    };

    

    


}