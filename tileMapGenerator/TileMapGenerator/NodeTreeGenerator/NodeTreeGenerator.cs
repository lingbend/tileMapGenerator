namespace NodeTreeGenerator;

using TileMapGenerator;
using QuikGraph;
using System.Numerics;
using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using DFS = QuikGraph.Algorithms.Search.UndirectedDepthFirstSearchAlgorithm<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using QuikGraph.Algorithms;
using Mono.CompilerServices.SymbolWriter;
using System.Runtime.ExceptionServices;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using Newtonsoft.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

public class NodeTreeGenerator
{
    
    public HashSet<Vector2> Valid_directions {get; private set;}= [Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX,
     -Vector2.UnitY];
    // X = width, Y = height, Z = weight towards this size. Sizes will be on a scale between given points
    public List<Vector3> Room_Sizes_Weights{get; internal set;} = [new Vector3(10, 10, 5), new Vector3(15, 15, 3), new Vector3(20, 20, 1)];
    
    private Graph _graph = new Graph(false);
    private BinaryGrid _grid;

    internal Random _random{get; set;} = new Random();

    public Func<Graph, Dictionary<Vector2, Vertex>, Dictionary<int, int>, IEnumerable<(Vertex, int)>> Shaper{get; set;} = ((graph, backing, weight) =>
    {
        int size = graph.VertexCount;
        Dictionary<int, int> enum_numbering = new Dictionary<int, int>();
        List<int> nums = new List<int>();
        foreach (var degree in weight.Keys.OrderDescending())
        {
            enum_numbering.Add(degree,(int) (weight[degree]*.01 * size));
        }
        int size_error = size - enum_numbering.Values.Sum();
        foreach(int degree in enum_numbering.Keys)
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
        return backing.Values.Zip(nums);
    });


    public NodeTreeGenerator(int size)
    {
        _grid = new BinaryGrid((uint) size, (uint) size);
    }

    public void SetValidDirections(HashSet<Vector2> new_directions)
    {
        foreach (var direction in new_directions)
        {
            if (direction.X + direction.Y != 1 && direction.X + direction.Y != 2 && direction.X != 1 && direction.Y != 1 && direction != Vector2.Zero)
            {
                throw new ArgumentException("Direction must not be zero vector. Must have components of 1 or 0.");
            }
        }
    }

    internal (Graph, bool) GenerateNodeTree(List<int> nodes_per_level, Dictionary<int, int> degree_weights, bool central=false, int depth=1)
    {  
        int size = (int) Math.Pow(4*nodes_per_level[0], .5);
        (_graph, Dictionary<Vector2, Vertex> backing_dictionary) = GenerateFilledGraph(size, size);
        (_graph, backing_dictionary, List<Vector2> holes) = CutVerticesDownTo(_graph, backing_dictionary, nodes_per_level[0], new Vector2(size, size));
        (_graph, backing_dictionary, bool result) = ReworkDegreeDistribution(degree_weights, _graph, backing_dictionary, holes, new Vector2(size, size));
        return (_graph, result);
    }

    private (Graph, Dictionary<Vector2, Vertex>, bool) ReworkDegreeDistribution(Dictionary<int, int> degree_weights, Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, List<Vector2> holes, Vector2 size)
    {
        bool possible = true;
        int min = degree_weights.Keys.Min();
        int max = degree_weights.Keys.Max();

        (graph, backing_dictionary, holes, bool local_success) = ForceEdgesToRange(graph, backing_dictionary, holes, min, max, size);
        possible &= local_success;
        
        Graph min_span_tree = GetMinSpanTree(graph);
        Graph min_span_tree_copy = GetMinSpanTree(graph);
        Dictionary<int, int> degree_percents = CalculateDegreePercents(degree_weights);
        HashSet<Vertex> processed_vertices = new HashSet<Vertex>();
        HashSet<Edge> processed_edges = new HashSet<Edge>();
        HashSet<Vertex> deleted_vertices = new HashSet<Vertex>();
        HashSet<Vertex> new_vertices = new HashSet<Vertex>();

        foreach ((Vertex next_vertex, int target_degree) in GetNextVertexDegreePair(min_span_tree_copy, new Dictionary<Vector2, Vertex>(backing_dictionary), degree_percents))
        {
            processed_vertices.Add(next_vertex);
            if (deleted_vertices.Contains(next_vertex))
            {
                continue;
            }
            
            int degree = 0;
            TryGetExistingEdges(ref possible, min_span_tree, next_vertex, target_degree, ref degree);

            TryTransplantEdges(ref graph, backing_dictionary, ref possible, min_span_tree, processed_vertices, processed_edges, next_vertex, target_degree, ref degree);

            var (deleted, new_verts) = TryTransplantVertices(ref graph, backing_dictionary, ref possible, min_span_tree, processed_vertices, processed_edges, next_vertex, target_degree, ref degree);

            deleted_vertices.UnionWith(deleted);
            new_vertices.UnionWith(new_verts);

            processed_edges.UnionWith(next_vertex.Edges);

        }
        var edge_copy = new List<Edge>(graph.Edges);
        foreach (var edge in edge_copy)
        {
            if (!min_span_tree.ContainsEdge(edge))
            {
                graph = RemoveEdge(graph, edge);
            }
        }
        return (min_span_tree, backing_dictionary, possible);
    }

    private void TryTransplantEdges(ref Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, ref bool possible, Graph min_span_tree, HashSet<Vertex> processed_vertices, HashSet<RoomEdge<Vector2>> processed_edges, Vertex next_vertex, int target_degree, ref int degree)
    {

        while (degree < target_degree)
        {


            var nearby_vertices = new HashSet<Vertex>(GetAdjacentVertices(backing_dictionary, next_vertex.Weight));
            var connected_vertices = new HashSet<Vertex>(graph.AdjacentVertices(next_vertex));

            if (nearby_vertices.IsSubsetOf(connected_vertices))
            {
                possible = false;
                break;
            }

            HashSet<Vertex> sacrificial_vertices = GetTarjanNonArticulatingPoints(graph, backing_dictionary);
            sacrificial_vertices.ExceptWith(processed_vertices);

            if (sacrificial_vertices.Count != 0)
            {
                var sacrificial_edges = GetAllEdges(sacrificial_vertices);
                var sacrificial_edge = ChooseRandom(sacrificial_edges);

                if (sacrificial_edge == null)
                {
                    break;
                }

                while (processed_edges.Contains(sacrificial_edge) || sacrificial_edge.Target.Degree == 1 || sacrificial_edge.Source.Degree == 1 || min_span_tree.ContainsEdge(sacrificial_edge))
                {
                    sacrificial_edge = ChooseRandom(sacrificial_edges);

                    if (sacrificial_edge == null || sacrificial_edges.Count == 0)
                    {
                        break;
                    }
                }

                if (sacrificial_edge != null && !processed_edges.Contains(sacrificial_edge) && !min_span_tree.ContainsEdge(sacrificial_edge))
                {
                    graph = RemoveEdge(graph, sacrificial_edge);

                }

            }

            foreach (var vertex in nearby_vertices)
            {
                if (!connected_vertices.Contains(vertex))
                {
                    min_span_tree.AddEdge(next_vertex.ConnectToVertex(vertex, next_vertex.Weight - vertex.Weight));
                    degree++;
                    break;
                }
            }
        }
    }

    private (HashSet<Vertex>, HashSet<Vertex>) TryTransplantVertices(ref Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, ref bool possible, Graph min_span_tree, HashSet<Vertex> processed_vertices, HashSet<RoomEdge<Vector2>> processed_edges, Vertex next_vertex, int target_degree, ref int degree)
    {
        HashSet<Vertex> deleted_vertices = new HashSet<Vertex>();
        HashSet<Vertex> new_vertices = new HashSet<Vertex>();
        while (degree < target_degree)
        {
            HashSet<Vertex> sacrificial_vertices = GetTarjanNonArticulatingPoints(graph, backing_dictionary);

            sacrificial_vertices.ExceptWith(processed_vertices);
            sacrificial_vertices.ExceptWith(GetAllVertices(processed_edges));

            if (sacrificial_vertices.Count != 0)
            {
                Vertex sacrificial_vertex = ChooseRandom(sacrificial_vertices!)!;
                deleted_vertices.Add(sacrificial_vertex);
                graph.RemoveVertex(sacrificial_vertex);
                if (sacrificial_vertex == null)
                {
                    throw new Exception();
                }

                if (min_span_tree.ContainsVertex(sacrificial_vertex))
                {
                    min_span_tree.RemoveVertex(sacrificial_vertex);
                }
                
                foreach (Edge edge in sacrificial_vertex.Edges)
                {
                    if (min_span_tree.ContainsEdge(edge))
                    {
                        min_span_tree.RemoveEdge(edge);
                    }
                    RemoveEdge(graph, edge);
                }
                
                backing_dictionary.Remove(sacrificial_vertex.Weight);
                HashSet<Vector2> available_directions = new HashSet<Vector2>(GetAvailableDirections(next_vertex, backing_dictionary));
                Vector2 direction = ChooseRandom(available_directions);
                Vertex replacement_vertex = new Vertex(next_vertex.Weight + direction);
                new_vertices.Add(replacement_vertex);
                Edge connection = next_vertex.ConnectToVertex(replacement_vertex, direction);
                graph.AddVerticesAndEdge(connection);
                min_span_tree.AddVerticesAndEdge(connection);
                backing_dictionary.Add(replacement_vertex.Weight, replacement_vertex);
                processed_edges.Add(connection);
                degree++;

            }
            else
            {
                possible = false;
                break;
            }
        }  
        return (deleted_vertices, new_vertices);  
    }

    private IList<Edge> GetAllEdges(IEnumerable<Vertex> vertices)
    {
        List<Edge> edges = new List<Edge>();
        foreach (var vertex in vertices)
        {
            foreach (var edge in vertex.Edges)
            {
                edges.Add(edge);
            }
        }
        return edges;
    }

    internal ISet<Vector2> GetAvailableDirections(Vertex node, Dictionary<Vector2, Vertex> backing_dictionary)
    {
        HashSet<Vector2> used_directions = new HashSet<Vector2>();
        foreach (var edge in node.Edges)
        {
            if (edge.Source == node)
            {
                used_directions.Add(edge.Weight);
            }
            else
            {
                used_directions.Add(-edge.Weight);
            }
        }
        HashSet<Vector2> available_directions = new HashSet<Vector2>(Valid_directions);
        available_directions.ExceptWith(used_directions);
        used_directions.Clear();
        
        Vector2 relative_loc = node.Weight;
        foreach (var direction in available_directions)
        {
            Vector2 new_direction = direction + relative_loc;
            if (backing_dictionary.ContainsKey(new_direction))
            {
                used_directions.Add(direction);
            }
        }
        available_directions.ExceptWith(used_directions);

        return available_directions;
    }

    private ISet<Vertex> GetAllVertices(IEnumerable<Edge> edges)
    {
        HashSet<Vertex> vertices = new HashSet<Vertex>();
        foreach (var edge in edges)
        {
            vertices.Add(edge.Source);
            vertices.Add(edge.Target);
        }
        return vertices;
    }

    private void TryGetExistingEdges(ref bool possible, Graph min_span_tree, Vertex next_vertex, int target_degree, ref int degree)
    {
        foreach (var edge in next_vertex.Edges)
        {
            if (min_span_tree.ContainsEdge(edge))
            {
                degree++;
            }
        }
        if (degree > target_degree)
        {
            possible = false;
        }

        foreach (var edge in next_vertex.Edges)
        {
            if (degree == target_degree)
            {
                break;
            }
            if (!min_span_tree.ContainsEdge(edge))
            {
                min_span_tree.AddEdge(edge);
                degree++;
            }
        }
    }

    private IEnumerable<(Vertex, int)> GetNextVertexDegreePair(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, Dictionary<int, int> degree_percents)
    {
        return Shaper(graph, backing_dictionary, degree_percents);
    }


    private Graph GetMinSpanTree(Graph graph)
    {
        Graph min_span_tree = new Graph(false);
        var tree = new QuikGraph.Algorithms.MinimumSpanningTree.PrimMinimumSpanningTreeAlgorithm<Vertex, Edge>(graph, (Edge edge)=>(double) Math.Abs((edge.Source.Weight-edge.Target.Weight).Length()));
        tree.TreeEdge += ((edge) =>
        {
            min_span_tree.AddVerticesAndEdge(edge);
        });
        tree.Compute();
        return min_span_tree;
    }

    private Dictionary<int, int> CalculateDegreePercents(Dictionary<int, int> degree_weights)
    {
        int total_weight = degree_weights.Values.Sum();
        Dictionary<int, int> degree_percents = new Dictionary<int, int>();
        foreach (int degree in degree_weights.Keys)
        {
            degree_percents.Add(degree, (int) (degree_weights[degree]*100.0/total_weight));
        }
        return degree_percents;
    }

    

    private (Graph, Dictionary<Vector2, Vertex>, List<Vector2>, bool) ForceEdgesToRange(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, List<Vector2> holes, int min, int max, Vector2 size)
    {
        List<Vertex> unforced_vertices = new List<Vertex>();
        bool successful = true;
        foreach (var vertex in graph.Vertices)
        {
            var disposable_vertices = GetTarjanNonArticulatingPoints(graph, backing_dictionary);
            if (disposable_vertices.Contains(vertex))
            {
                // List<Edge> edges_copy = vertex.Edges;
                while (vertex.Degree > max)
                {
                    var edge = ChooseRandom<Edge>(vertex.Edges);
                    graph = RemoveEdge(graph, edge);
                }

                if (vertex.Degree < min)
                {
                    HashSet<Vector2> current_directions = new HashSet<Vector2>();
                    foreach (var edge in vertex.Edges)
                    {
                        current_directions.Add(edge.Weight);
                    }
                    HashSet<Vector2> possible_directions = new HashSet<Vector2> (Valid_directions);
                    possible_directions.ExceptWith(current_directions);
                    List<Vector2> possible_directions_list = new List<Vector2>(possible_directions);
                    while (vertex.Degree < min)
                    {
                        if (possible_directions_list.Count == 0)
                        {
                            unforced_vertices.Add(vertex);
                            break;
                        }

                        var chosen_direction = ChooseRandom(possible_directions_list);
                        if (FindFirstVertexInDirection(backing_dictionary, vertex.Weight, chosen_direction, size, out Vector2 result))
                        {
                            graph.AddEdge(vertex.ConnectToVertex(backing_dictionary[result], backing_dictionary[result].Weight-vertex.Weight));
                        }
                    }
                }
            }
            else
            {
                unforced_vertices.Add(vertex);
            }
        }
        foreach (var vertex in unforced_vertices)
        {
            int target_degree;
            if (vertex.Degree < min)
            {
                target_degree = min;
            }
            else
            {
                target_degree = max;
            }
            (Vector2? hole, bool local_success) = GetHoleOfDegreeOrGreater(backing_dictionary, holes, target_degree);
            if (local_success)
            {
                Vector2 hole_full = (Vector2) hole!;
                holes.Remove(hole_full);
                backing_dictionary.TryGetValue(hole_full, out Vertex value);
                foreach (Edge edge in value?.Edges ?? [])
                {
                    graph = RemoveEdge(graph, edge);
                    throw new Exception("garbage");
                }
                if (value != null)
                {
                    graph.RemoveVertex(backing_dictionary[hole_full]);
                    backing_dictionary.Remove(hole_full);
                }
                


                Vertex new_vertex = new Vertex(hole_full);
                backing_dictionary.Add(hole_full, new_vertex);
                graph.AddVertex(new_vertex);
                var adjacent_vertices = GetAdjacentVertices(backing_dictionary, hole_full);
                if (adjacent_vertices.Count == 0)
                {
                    throw new Exception();
                }
                while (new_vertex.Degree != target_degree)
                {
                    var choice = ChooseRandom(adjacent_vertices);
                    graph.AddEdge(new_vertex.ConnectToVertex(choice, choice.Weight - new_vertex.Weight));
                }
            }
            else
            {
                successful = false;
            }
        }
        return (graph, backing_dictionary, holes, successful);
    }

    private Graph RemoveEdge(Graph graph, Edge edge)
    {
        graph.RemoveEdge(edge);
        edge.Source.RemoveEdge(edge);
        edge.Target.RemoveEdge(edge);
        edge.Source = null;
        edge.Target = null;
        return graph;
    }

    private (Vector2?, bool) GetHoleOfDegreeOrGreater(Dictionary<Vector2, Vertex> backing_dictionary, List<Vector2> holes, int degree_min)
    {
        Span<Vector2> holes_copy = new Span<Vector2>(holes.ToArray());
        _random.Shuffle(holes_copy);
        foreach (var hole in holes_copy)
        {
            if (GetAdjacentVertices(backing_dictionary, hole).Count >= degree_min)
            {
                return (hole, true);
            }
        }
        return (null, false);
    }

    private List<Vertex> GetAdjacentVertices(Dictionary<Vector2, Vertex> backing_dictionary, Vector2 origin)
    {
        List<Vertex> adjacent_vertices = new List<Vertex>();
        foreach (var dir in Valid_directions)
        {
            if (backing_dictionary.TryGetValue(dir+origin, out Vertex? val))
            {
                adjacent_vertices.Add(val);
            }
        }
        return adjacent_vertices;
    }

    private bool FindFirstVertexInDirection(Dictionary<Vector2, Vertex> backing_dictionary, Vector2 origin, Vector2 direction, Vector2 size, out Vector2 result)
    {
        Vector2 current_index = origin;
        try
        {
            while (current_index.X > 0 && current_index.Y > 0 && current_index.X < size.X && current_index.Y < size.Y)
            {
                current_index += direction;
                if (backing_dictionary.ContainsKey(current_index))
                {
                    result = current_index;
                    return true;
                }

            }
        }
        catch
        {
            result = new Vector2(-1, -1);
            return false;
        }
        result = new Vector2(-1, -1);
        return false;
    }


    internal (Graph, Dictionary<Vector2, Vertex>, List<Vector2>) CutVerticesDownTo(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, int target_number, Vector2 size)
    {
        List<Vector2> holes = new List<Vector2>();

        while (graph.VertexCount > target_number)
        {
            HashSet<Vertex> disposable_vertices = GetTarjanNonArticulatingPoints(graph, backing_dictionary);
            Vertex choice = ChooseWeightedRandom(disposable_vertices, (vert) =>
            {
                switch (vert.Degree)
                {
                    case 0:
                        return 10;
                    case 1:
                        return 2;
                    case 2:
                        return 5;
                    default:
                        return 1;
                }
            }, 5);

            foreach (Edge edge in choice.Edges.ToArray())
            {
                graph = RemoveEdge(graph, edge);
            }
            graph.RemoveVertex(choice);
            backing_dictionary.Remove(choice.Weight);
            holes.Add(choice.Weight);

        }
        return (graph, backing_dictionary, holes);
    }

    private Graph PatchHoles(List<Graph> components, Dictionary<Vector2, Vertex> backing_dictionary)
    {
        Graph unified_graph = new Graph(false);
        unified_graph.AddVerticesAndEdgeRange(components[0].Edges);
        
        foreach (var comp in components[1..])
        {
            var vertex = comp.Vertices.First();
            var adjacent_vertices = GetAdjacentVertices(backing_dictionary, vertex.Weight);
            foreach (var adj_vertex in adjacent_vertices)
            {
                if (unified_graph.ContainsVertex(adj_vertex))
                {
                    unified_graph.AddVerticesAndEdge(vertex.ConnectToVertex(adj_vertex, adj_vertex.Weight - vertex.Weight));
                    unified_graph.AddVerticesAndEdgeRange(comp.Edges);
                    continue;
                }
            }
            unified_graph.AddVerticesAndEdgeRange(comp.Edges);
        }
        return unified_graph;
    }

    internal HashSet<Vertex> GetTarjanNonArticulatingPoints(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary)
    {
        Vertex? root = null;
        int discovery_time = 1;
        HashSet<Vertex> articulation_points = new HashSet<Vertex>();
        Dictionary<Vertex, (int, int)> visited_points = new Dictionary<Vertex, (int, int)>();
        
        var test_connect = new QuikGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<Vertex, Edge>(graph);
        test_connect.Compute();
        if (test_connect.ComponentCount > 1)
        {
            List<Graph> patching_graphs = new List<Graph>();
            foreach (var (vertex, component) in test_connect.Components)
            {
                if (patching_graphs.Count < component + 1)
                {
                    patching_graphs.Add(new Graph(false));
                }
                patching_graphs[component].AddVerticesAndEdgeRange(vertex.Edges);
                patching_graphs[component].AddVertex(vertex);
            }
            graph = PatchHoles(patching_graphs, backing_dictionary);
        }
        test_connect = new QuikGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<Vertex, Edge>(graph);
        test_connect.Compute();
        if (test_connect.ComponentCount > 1)
        {
            throw new Exception("too many components in Tarjan's");
        }
        var search = new DFS(graph);
        search.StartVertex += ((vert) =>
        {
            if (root == null)
            {
                root = vert;
            }
        });
        search.InitializeVertex += ((vert) =>
        {
            ((Vertex) vert)["children"] = new List<Edge>();
        });
        search.DiscoverVertex += (vert =>
        {
            visited_points.Add(vert, (discovery_time, discovery_time));
            discovery_time++;
        });
        search.BackEdge += ((obj, args) =>
        {
            Vertex vert;
            Vertex other_vert;
            if (visited_points[args.Edge.Source].Item1 > visited_points[args.Edge.Target].Item1)
            {
                vert = args.Edge.Source;
                other_vert = args.Edge.Target;
            }
            else
            {
                vert = args.Edge.Target;
                other_vert = args.Edge.Source;
            }
            
            visited_points[(Vertex)vert] = (visited_points[(Vertex)vert].Item1, Math.Min(visited_points[other_vert].Item1, visited_points[(Vertex)vert].Item2));
        });

        search.TreeEdge += ((obj, args) =>
        {
            Vertex vert;
            var temp = graph;
            if (((DFS)obj).GetVertexColor(args.Edge.Source) == GraphColor.Gray){
                vert = args.Edge.Source;
            }
            else
            {
                vert = args.Edge.Target;
            }
            if (vert["children"] == null)
            {
                vert["children"] = new List<Edge>();
            }
            ((List<Edge>)vert["children"]!).Add(args.Edge);
        });
        search.FinishVertex += ((vert) =>
        {
            if (vert == root)
            {
                if (((List<Edge>)((Vertex)vert)["children"] ?? []).Count() >= 2)
                {
                    articulation_points.Add(vert);
                }
            }
            else
            {
                foreach (var edge in ((List<Edge>)((Vertex)vert)["children"]) ?? [])
                {
                    
                    Vertex other_vert;
                    if (vert == edge.Source)
                    {
                        other_vert = edge.Target;
                    }
                    else
                    {
                        other_vert = edge.Source;
                    }
                    if (visited_points[other_vert].Item2 < visited_points[vert].Item2)
                    {
                        visited_points[vert] = (visited_points[vert].Item1, visited_points[other_vert].Item2);
                    }
                    if (visited_points[other_vert].Item2 >= visited_points[vert].Item1)
                    {
                        articulation_points.Add(vert);
                    }
                }
            }
            
        });

        search.Compute();

        HashSet<Vertex> disposable_vertices = new HashSet<Vertex>(graph.Vertices);
        disposable_vertices.ExceptWith(articulation_points);
        return disposable_vertices;
    }

    internal (Graph, Dictionary<Vector2, Vertex>) GenerateFilledGraph(int rows, int cols)
    {
        Dictionary<Vector2, Vertex> backing_dictionary = new Dictionary<Vector2, Vertex>();
        Graph graph = new Graph(false);
        List<List<Vertex>> vertex_rows = new List<List<Vertex>>();
        List<Edge> edges = new List<Edge>();
        for (uint row = 1; row <= rows; row++)
        {
            vertex_rows.Add(new List<Vertex>());
            var first_row_vertex = new Vertex(new Vector2(row, 1));
            graph.AddVertex(first_row_vertex);
            vertex_rows[(int) row-1].Add(first_row_vertex);
            backing_dictionary.Add(first_row_vertex.Weight, first_row_vertex);

            for (uint col = 2; col <= cols; col++)
            {
                var new_vertex = new Vertex(new Vector2(row, col));
                vertex_rows[(int) row-1].Add(new_vertex);
                edges.Add(vertex_rows[(int) row-1][(int) col-2].ConnectToVertex(new_vertex, new_vertex.Weight - vertex_rows[(int) row-1][(int) col-2].Weight));
                backing_dictionary.Add(new_vertex.Weight, new_vertex);
            }
        }
        
        for (int i = 0; i < vertex_rows.Count - 1; i++)
        {
            for (int j = 0; j < vertex_rows[i].Count; j++)
            {
                edges.Add(vertex_rows[i][j].ConnectToVertex(vertex_rows[i+1][j], vertex_rows[i+1][j].Weight - vertex_rows[i][j].Weight));
            }
        }
        graph.AddVerticesAndEdgeRange(edges);
        return (graph, backing_dictionary);
    }

    private T? ChooseRandom<T>(ICollection<T?> coll)
    {
        if (coll.Count == 0)
        {
            return default(T);
        }
        int rand = _random.Next(coll.Count - 1);
        T item = coll.ToArray()[rand];
        coll.Remove(item);
        return item;
    }

    private T ChooseWeightedRandom<T>(ICollection<T> coll, Func<T, int> weighter, int iterations = 3)
    {
        HashSet<T> collection_copy = new HashSet<T>(coll);
        HashSet<T> choices = new HashSet<T>();
        for (int i = 0; i < iterations; i++)
        {
            if (collection_copy.Count == 0)
            {
                break;
            }
            choices.Add(ChooseRandom(collection_copy));
        }
        return choices.MaxBy(weighter);
    }

    // internal Vector2 GetMaxRelativeNestedGraphSize(Graph nested_graph)
    // {
    //     Vector2 layer_min = Vector2.Zero;
    //     Vector2 layer_max = Vector2.Zero;
    //     Vector2 max_new_vector = Vector2.One;
    //     foreach (var node in nested_graph.Vertices)
    //     {
    //         layer_min = Vector2.Min(layer_min, node.Weight);
    //         layer_max = Vector2.Max(layer_max, node.Weight);

    //         if (node.InnerGraph != null)
    //         {
    //             var temp_vector = GetMaxRelativeNestedGraphSize((Graph) node.InnerGraph!);
    //             max_new_vector = Vector2.Max(max_new_vector, temp_vector);                
    //         }
    //     }
    //     Vector2 layer_size = layer_max - layer_min + Vector2.One;
    //     return layer_size * max_new_vector;
    // }

    // internal Graph RemapNodes(Graph nested_graph)
    // {
    //     List<(Vertex, Graph)> graphs = new List<(Vertex, Graph)>();
    //     var vertices = nested_graph.Vertices;
    //     if (vertices.First().InnerGraph != null)
    //     {
    //         foreach (var vertex in vertices)
    //         {
    //             graphs.Add((vertex, RemapNodes((Graph)vertex.InnerGraph!)));
    //         }
    //     }
    //     else
    //     {
    //         return nested_graph;
    //     }

    //     Vector2 layer_min = Vector2.Zero;
    //     Vector2 layer_max = Vector2.Zero;
    //     foreach (var graph in graphs)
    //     {
    //         var node_weight = GetMaxRelativeNestedGraphSize(graph.Item2);
    //         layer_min = Vector2.Min(layer_min, node_weight);
    //         layer_max = Vector2.Max(layer_max, node_weight);
    //     }
    //     Vector2 layer_size = layer_max - layer_min + Vector2.One;
    //     Graph new_graph = new Graph();
    //     HashSet<(Vertex, Vertex)> heads = UnpackGraphWeights(graphs, layer_size, new_graph);
    //     AttachHeadVerticesTogether(layer_size, new_graph, heads);
    //     return new_graph;
    // }

    // internal HashSet<(Vertex, Vertex)> UnpackGraphWeights(List<(Vertex, Graph)> graphs, Vector2 layer_size, Graph new_graph)
    // {
    //     HashSet<(Vertex, Vertex)> heads = new HashSet<(Vertex, Vertex)>();
    //     foreach (var (vertex, graph) in graphs)
    //     {
    //         UpdateVertexWeights(graph.Vertices, layer_size);
    //         UpdateEdgeWeights(graph.Edges, layer_size);

    //         foreach (var edge in graph.Edges)
    //         {
    //             new_graph.AddVertex(edge.Source);
    //             new_graph.AddVertex(edge.Target);
    //             new_graph.AddEdge(edge);
    //             if (edge.Source.Center)
    //             {
    //                 heads.Add((vertex, edge.Source));
    //             }
    //             else if (edge.Target.Center)
    //             {
    //                 heads.Add((vertex, edge.Target));
    //             }
    //         }
    //     }
    //     return heads;
    // }

    // private void UpdateEdgeWeights(IEnumerable<Edge> edges, Vector2 size)
    // {
    //     foreach (var edge in edges)
    //     {
    //         edge.Weight = UnpackWeightVector(edge.Weight, size);
    //     }
    // }

    // internal void UpdateVertexWeights(IEnumerable<Vertex> vertices, Vector2 size)
    // {
    //     foreach (var vertex in vertices)
    //     {
    //         if (vertex.Center)
    //         {
    //             vertex.Weight = vertex.Weight * size + GetOffsetWeight(vertex.Weight, size);
    //         }
    //         else
    //         {
    //             vertex.Weight = UnpackWeightVector(vertex.Weight, size) + GetOffsetWeight(vertex.Weight, size);
    //         }
    //     }
    // }

    // internal void AttachHeadVerticesTogether(Vector2 layer_size, Graph new_graph, HashSet<(Vertex, Vertex)> heads)
    // {
    //     HashSet<Edge> deferred_edges = new HashSet<Edge>();
    //     foreach (var (vertex, head) in heads)
    //     {
    //         foreach (var edge in vertex.Edges)
    //         {
    //             if (!new_graph.Edges.Contains(edge))
    //             {
    //                 edge.Weight = UnpackWeightVector(edge.Weight, layer_size);
    //                 deferred_edges.Add(edge);
    //             }
                
    //             if (edge.Source == vertex)
    //             {
    //                 edge.Source = head;
    //             }
    //             else
    //             {
    //                 edge.Target = head;
    //             }
    //         }
    //     }
    //     foreach (var edge in deferred_edges)
    //     {
    //         if (!new_graph.AddEdge(edge))
    //         {
    //             throw new Exception("edge not added");
    //         }
    //     }
    // }

    // private Vector2 UnpackWeightVector(Vector2 weight, Vector2 layer_size)
    // {
    //     return Vector2.Normalize(weight) * layer_size;
    // }

    // private Vector2 GetOffsetWeight(Vector2 center, Vector2 layer_size)
    // {
    //     return center * layer_size;
    // }

    // internal Graph GenerateDecentralizedRegion(List<int> sizes, Vector2 center, int max, int min, int current_depth=1)
    // {
    //     var subgrid = new BinaryGrid((uint) sizes[0]*2, (uint) sizes[0]*2);
    //     var subgraph = new Graph();
    //     Vertex current_node = new Vertex(center);
    //     current_node["name"] = $"head depth: {current_depth}, loc: {center}";
    //     current_node.Center = true;
    //     subgraph.AddVertex(current_node);
    //     subgrid.SetCell((uint) current_node.Weight.Y, (uint) current_node.Weight.X, 1U);
    //     if (current_depth != 1)
    //     {
    //         current_node.InnerGraph = GenerateDecentralizedRegion(sizes[1..], center, max, min, current_depth - 1);
    //     }
    //     List<Vector2> directions = new List<Vector2> (GetAvailableDirections(current_node));
    //     Vector2 direction;
    //     int target_degree = _random.Next(min, max+1)-1;
    //     if (current_depth == 1)
    //     {
    //         target_degree++;
    //     }
    //     while (subgraph.VertexCount != sizes[0])
    //     {
            
    //         if (directions.Count == 0 || current_node.Degree == target_degree)
    //         {
    //             (directions, current_node) = GetNextLeaf(subgraph, max);
    //             target_degree = _random.Next(min, max+1);
    //         }
    //         direction = ChooseRandom(directions);

    //         (Vertex vert, _) = AddNode(current_node, direction, subgraph, subgrid);
    //         if (current_depth != 1)
    //         {
    //             vert.InnerGraph = GenerateDecentralizedRegion(sizes[1..], center, max, min, current_depth - 1);
    //         }
            
    //     }
    //     return subgraph;
    // }

    // internal (List<Vector2>, Vertex) GetNextLeaf(Graph graph, int max)
    // {
    //     List<Vector2> new_directions = new List<Vector2>();
    //     List<Vertex> vertices = new List<Vertex>(graph.Vertices);
    //     Vertex new_leaf;
    //     foreach (var vertex in vertices)
    //     {
    //         new_leaf = vertex;
    //         new_directions =  new List<Vector2> (GetAvailableDirections(new_leaf));
    //         if (new_directions.Count != 0 && new_leaf.Edges.Count < max)
    //         {
    //             return (new_directions, new_leaf);
    //         }
    //     }
    //     throw new Exception("Could not locate next leaf");
    // }

    // private (Vertex, Edge) AddNode(Vertex source_vertex, Vector2 direction_weight, Graph graph, BinaryGrid grid)
    // {
    //     Vertex new_vertex = new Vertex(source_vertex.Weight + direction_weight);
    //     new_vertex["name"] = $"subnode {source_vertex.Weight + direction_weight}";
    //     Edge new_edge = source_vertex.ConnectToVertex(new_vertex);
    //     new_edge.Weight = direction_weight;
    //     grid.SetCell((uint) new_vertex.Weight.Y, (uint) new_vertex.Weight.X, 1U);
    //     graph.AddVertex(new_vertex);
    //     graph.AddEdge(new_edge); 
    //     return (new_vertex, new_edge);
    // }

    // internal ISet<Vector2> GetAvailableDirections(Vertex node)
    // {
    //     HashSet<Vector2> used_directions = new HashSet<Vector2>();
    //     foreach (var edge in node.Edges)
    //     {
    //         if (edge.Source == node)
    //         {
    //             used_directions.Add(edge.Weight);
    //         }
    //         else
    //         {
    //             used_directions.Add(-edge.Weight);
    //         }
    //     }
    //     HashSet<Vector2> available_directions = new HashSet<Vector2>(Valid_directions);
    //     available_directions.ExceptWith(used_directions);
    //     used_directions.Clear();
        
    //     Vector2 relative_loc = node.Weight;
    //     foreach (var direction in available_directions)
    //     {
    //         Vector2 new_direction = direction + relative_loc;
    //         if (new_direction.X > 0 && new_direction.Y > 0 &&
    //          _grid.GetCell((uint) new_direction.Y, (uint) new_direction.X) == 1)
    //         {
    //             used_directions.Add(direction);
    //         }
    //         else if (new_direction.X <= 0 || new_direction.Y <= 0)
    //         {
    //             used_directions.Add(direction);
    //         }
    //     }
    //     available_directions.ExceptWith(used_directions);

    //     return available_directions;
    // }

        // private int GetGraphDegree(Graph graph)
        // {
        //     int degrees = 0;
        //     foreach (var vertex in graph.Vertices)
        //     {
        //         degrees += vertex.Degree;
        //     }
        //     return degrees;
        // }

        // internal Graph GenerateCentralRegion(List<int> sizes, Vector2 center, int max, int min, int current_depth=1)
        // {
        //     // throw new NotImplementedException();
        //     return GenerateDecentralizedRegion(sizes, center, max, min, current_depth);
        // }

        // internal void UnnestGraph()
        // {
        //     _graph= RemapNodes(_graph);
        //     Vector2 size = GetMaxRelativeNestedGraphSize(_graph);
        //     _grid = new BinaryGrid((uint) size.Y, (uint) size.X);
        //     foreach (var vertex in _graph.Vertices)
        //     {
        //         _grid.SetCell((uint) vertex.Weight.Y, (uint) vertex.Weight.X, 1U);
        //     }
        // }
    // private static void ValidateInputs(int min, int max, List<int> zone_node_counts, int depth)
    // {
    //     if (depth <= 0 || zone_node_counts.Count < depth || min < 0 || max <= 0 ||
    //              min > max || zone_node_counts.Any((x) => x <= 0))
    //     {
    //         throw new ArgumentException($"Bad Arguments: depth: {depth},"+
    //          $"room counts: {string.Concat(zone_node_counts)}, min degree: {min}, max degree: {max}");
    //     }
    // }

        // private Graph GenerateDecentralizedRegion(List<int> sizes)
    // {
    //     return GenerateDecentralizedRegion(sizes, Vector2.Zero);
    // }

    //     private void CheckGraphFeasibility(int min, int max, List<int> zone_node_counts, bool connected)
    // {
    //     if (min == max && (max % 2) == 1 && zone_node_counts.Any((x) => (x % 2) == 1))
    //     {
    //         throw new ArgumentException("Impossible graph");
    //     }
    //     else if (connected && zone_node_counts.Any((x) => x*max*.5 < x - 1))
    //     {
    //         throw new ArgumentException("Impossible Graph");
    //     }
    //     // else if (min > 5)
    //     // {
    //     //     throw new ArgumentException("Nonplanar graph");
    //     // }
    //     // foreach (int zone_num in zone_node_counts)
    //     // {
    //     //     if ((zone_num - 1) < min || (min > (6 - (12 / zone_num)) && zone_num >= 3))
    //     //     {
    //     //         throw new ArgumentException("Likely non-planar or impossible graph");
    //     //     }
    //     // }
    // }

    //     private (int, int, List<int>) CorrectGraphFeasibility(int min, int max, List<int> zone_node_counts)
    // {
    //     if (min == max && max % 2 == 1)
    //     {
    //         if (min < 4)
    //         {
    //             min++;
    //             max++;
    //         }
    //         else
    //         {
    //             min--;
    //             max--;
    //         }
    //     }
    //     if (min > 5)
    //     {
    //         max -= min - 5;
    //         min -= min - 5;
    //     }
        
        
    //     for (int i = 0; i < zone_node_counts.Count; i++)
    //     {
    //         if ((zone_node_counts[i] - 1) < min)
    //         {
    //             zone_node_counts[i] = min + 1;
    //         }
    //         if (min >( 6 - (12 / zone_node_counts[i])) && zone_node_counts[i] >= 3)
    //         {
    //             zone_node_counts[i] = (int) Math.Ceiling((double) (12.0/(6.0-min)));
    //         }
    //     }
    //     return (min, max, zone_node_counts);
    // }
}

