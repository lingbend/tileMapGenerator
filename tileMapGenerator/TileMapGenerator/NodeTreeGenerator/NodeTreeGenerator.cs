namespace NodeTreeGenerator;

using TileMapGenerator;
using QuikGraph;
using System.Numerics;
using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using DFS = QuikGraph.Algorithms.Search.UndirectedDepthFirstSearchAlgorithm<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;

public class NodeTreeGenerator
{
    private Graph _graph = new Graph(false);

    public NodeTreeGeneratorSettings Settings {get; set;} = new NodeTreeGeneratorSettings();

    public NodeTreeGenerator(){}

    internal (Graph, bool) GenerateNodeTree(int num_rooms, bool central=false, int depth=1)
    {  
        int size = (int) Math.Pow(4.0*(num_rooms*(1.0+(Settings.InitialPaddingPercent/100.0))), .5);
        (_graph, Dictionary<Vector2, Vertex> backing_dictionary) = GenerateFilledGraph((int) Math.Ceiling(size*Settings.InitialRatio.Y),(int) Math.Ceiling(size*Settings.InitialRatio.X));
        return GenerateNodeTreeInner(num_rooms, size, ref backing_dictionary);
    }

    internal (Graph, bool) GenerateNodeTree(int num_rooms, Graph graph, bool central=false, int depth = 1)
    {
        int size = (int)Math.Pow(4.0 * (num_rooms * (1.0 + (Settings.InitialPaddingPercent / 100.0))), .5);
        _graph = graph;
        Dictionary<Vector2, Vertex> backing_dictionary = new();
        foreach (Vertex vertex in _graph.Vertices)
        {
            backing_dictionary.Add(vertex.Weight, vertex);
        }
        return GenerateNodeTreeInner(num_rooms, size, ref backing_dictionary);
    }

    private (Graph, bool) GenerateNodeTreeInner(int num_rooms, int size, ref Dictionary<Vector2, Vertex> backing_dictionary)
    {
        (_graph, backing_dictionary, List<Vector2> holes) = CutVerticesDownTo(_graph, backing_dictionary, (int)(num_rooms * (1.0 + (Settings.InitialPaddingPercent / 100.0))));
        (_graph, backing_dictionary, bool result) = ReworkDegreeDistribution(_graph, backing_dictionary, holes, new Vector2(size, size));
        if (Settings.InitialPaddingPercent != 0)
        {
            (_graph, backing_dictionary, _) = CutVerticesDownTo(_graph, backing_dictionary, num_rooms);
        }
        foreach (var processor in Settings.PostProcessors)
        {
            (_graph, backing_dictionary) = processor(_graph, backing_dictionary);
            (_graph, backing_dictionary) = CheckForHolesAndPatch(_graph, backing_dictionary);
        }
        return (_graph, result);
    }

    private (Graph, Dictionary<Vector2, Vertex>) RemoveDuplicates(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary)
    {
        foreach (Vertex vertex in backing_dictionary.Values)
        {
            LinkedList<Edge> edges = new();
            LinkedList<Edge> duplicate_edges = new();
            foreach (Edge edge in vertex.Edges)
            {
                foreach (Edge edge1 in edges)
                {
                    if (edge.Source == vertex)
                    {
                        if (edge.Target == edge1.Target || edge.Target == edge1.Source)
                        {
                            duplicate_edges.AddLast(edge1);
                            break;
                        }
                    }
                    else
                    {
                        if (edge.Source == edge1.Target || edge.Source == edge1.Source)
                        {
                            duplicate_edges.AddLast(edge1);
                            break;
                        }
                    }
                }
                
                edges.AddLast(edge);
            }
            foreach (Edge edge in duplicate_edges)
            {
                RemoveEdge(graph, edge);
            }
        }
        
        return (graph, backing_dictionary);
    }

    private (Graph, Dictionary<Vector2, Vertex>, bool) ReworkDegreeDistribution(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, List<Vector2> holes, Vector2 size)
    {
        Dictionary<int, int> degree_percents = Settings.degree_percents;
        bool possible = true;
        int min = degree_percents.Keys.Min();
        int max = degree_percents.Keys.Max();

        (graph, backing_dictionary, _, bool local_success) = ForceEdgesToRange(graph, backing_dictionary, holes, min, max, size);
        possible &= local_success;

        Graph min_span_tree = GetMinSpanTree(graph);
        Graph min_span_tree_copy = GetMinSpanTree(graph);
        // Dictionary<int, int> degree_percents = CalculateDegreePercents(degree_weights);

        HashSet<Vertex> processed_vertices = new HashSet<Vertex>();
        HashSet<Edge> processed_edges = new HashSet<Edge>();
        HashSet<Vertex> deleted_vertices = new HashSet<Vertex>();
        HashSet<Vertex> new_vertices = new HashSet<Vertex>();
        InnerReworkDegreeDistribution(ref graph, backing_dictionary, ref possible, min_span_tree, min_span_tree_copy, degree_percents, processed_vertices, processed_edges, deleted_vertices, new_vertices);
        Graph new_vertices_container = new Graph(false);
        new_vertices_container.AddVerticesAndEdgeRange(GetAllEdges(new_vertices));
        InnerReworkDegreeDistribution(ref graph, backing_dictionary, ref possible, min_span_tree, new_vertices_container, degree_percents, processed_vertices, processed_edges, deleted_vertices, new HashSet<Vertex>());
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

    private void InnerReworkDegreeDistribution(ref Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, ref bool possible, Graph min_span_tree, Graph min_span_tree_copy, Dictionary<int, int> degree_percents, HashSet<Vertex> processed_vertices, HashSet<RoomEdge<Vector2>> processed_edges, HashSet<Vertex> deleted_vertices, HashSet<Vertex> new_vertices)
    {
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
    }

    private void TryTransplantEdges(ref Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, ref bool possible, Graph min_span_tree, HashSet<Vertex> processed_vertices, HashSet<RoomEdge<Vector2>> processed_edges, Vertex next_vertex, int target_degree, ref int degree)
    {

        while (degree < target_degree)
        {


            var nearby_vertices = new HashSet<Vertex>(GetAdjacentVertices(backing_dictionary, next_vertex.Weight));
            var connected_vertices = new HashSet<Vertex>(graph.AdjacentVertices(next_vertex));

            if (nearby_vertices.IsSubsetOf(connected_vertices))
            {
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
                if (direction == Vector2.Zero)
                {
                    continue;
                }
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
        HashSet<Vector2> available_directions = new HashSet<Vector2>(Settings.ValidDirections);
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
        return Settings.Shaper(graph, backing_dictionary, degree_percents);
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
        List<(Vertex, int)> unforced_vertices = new List<(Vertex, int)>();
        bool successful = true;
        foreach (var (vertex, degree) in GetNextVertexDegreePair(graph, backing_dictionary, Settings.degree_percents))
        {
            var disposable_vertices = GetTarjanNonArticulatingPoints(graph, backing_dictionary);
            if (disposable_vertices.Contains(vertex))
            {
                while (graph.AdjacentDegree(vertex) > min)
                {
                    var edge = ChooseRandom<Edge>(vertex.Edges);
                    graph = RemoveEdge(graph, edge);
                }

                if (graph.AdjacentDegree(vertex) < max)
                {
                    HashSet<Vector2> current_directions = new HashSet<Vector2>();
                    foreach (var edge in vertex.Edges)
                    {
                        current_directions.Add(edge.Weight);
                    }
                    HashSet<Vector2> possible_directions = new HashSet<Vector2> (Settings.ValidDirections);
                    possible_directions.ExceptWith(current_directions);
                    List<Vector2> possible_directions_list = new List<Vector2>(possible_directions);
                    while (vertex.Degree < min)
                    {
                        if (possible_directions_list.Count == 0)
                        {
                            unforced_vertices.Add((vertex, degree));
                            break;
                        }

                        var chosen_direction = ChooseRandom(possible_directions_list);
                        if (backing_dictionary.TryGetValue(chosen_direction, out Vertex result))
                        {
                            graph.AddEdge(vertex.ConnectToVertex(backing_dictionary[result.Weight], backing_dictionary[result.Weight].Weight-vertex.Weight));
                        }
                    }
                }
            }
            else
            {
                unforced_vertices.Add((vertex, degree));
            }
        }
        foreach (var (vertex, degree) in unforced_vertices)
        {
            int target_degree;
            if (graph.AdjacentDegree(vertex) < min)
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
            if (GetConnectedComponents(graph).Count > 1)
                {
                    throw new Exception();
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
        Settings.Random.Shuffle(holes_copy);
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
        foreach (var dir in Settings.ValidDirections)
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


    internal (Graph, Dictionary<Vector2, Vertex>, List<Vector2>) CutVerticesDownTo(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary, int target_number)
    {
        List<Vector2> holes = new List<Vector2>();
        while (graph.VertexCount > target_number)
        {
            HashSet<Vertex> disposable_vertices = GetTarjanNonArticulatingPoints(graph, backing_dictionary);
            Vertex choice = ChooseWeightedRandom(disposable_vertices, Settings.degree_percents, Settings.WeightedVertexRemover, ((target_number*Settings.PruningSelectivityMultiplier)/5)+2);

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
            Vertex? vertex = null;
            foreach (Vertex potential_vert in comp.Vertices)
            {
                if (GetAdjacentVertices(backing_dictionary, potential_vert.Weight).Count > 0)
                {
                    foreach (var adj_vertex in GetAdjacentVertices(backing_dictionary, potential_vert.Weight))
                    {
                        if (unified_graph.ContainsVertex(adj_vertex))
                        {
                            vertex = potential_vert;
                            break;
                        }
                    }
                    if (vertex != null)
                    {
                        break;
                    }
                }
            }
            if (vertex == null)
            {
                List<(Vertex, Vertex)> alternative_pairs = new List<(Vertex, Vertex)>();
                bool found = false;
                foreach(Vertex vertex3 in unified_graph.Vertices)
                {
                    foreach(Vertex vertex4 in comp.Vertices)
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
                unified_graph.AddVerticesAndEdge(alternative_pairs[0].Item1.ConnectToVertex(alternative_pairs[0].Item2, alternative_pairs[0].Item2.Weight - alternative_pairs[0].Item1.Weight));
                unified_graph.AddVerticesAndEdgeRange(comp.Edges);
                
            }
            else
            {
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
        }
        return unified_graph;
    }

    private (Graph, Dictionary<Vector2, Vertex>) CheckForHolesAndPatch(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary)
    {
        List<Graph> patching_graphs = GetConnectedComponents(graph);
        if (patching_graphs.Count > 1)
        {
            graph = PatchHoles(patching_graphs, backing_dictionary);
        }
        return (graph, backing_dictionary);
    }

    internal static List<Graph> GetConnectedComponents(Graph graph)
    {
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
            return patching_graphs;
        }
        return [graph];
    }

    internal HashSet<Vertex> GetTarjanNonArticulatingPoints(Graph graph, Dictionary<Vector2, Vertex> backing_dictionary)
    {
        Vertex? root = null;
        int discovery_time = 1;
        HashSet<Vertex> articulation_points = new HashSet<Vertex>();
        Dictionary<Vertex, (int, int)> visited_points = new Dictionary<Vertex, (int, int)>();
        
        (graph, backing_dictionary) = CheckForHolesAndPatch(graph, backing_dictionary);
        var test_connect = new QuikGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<Vertex, Edge>(graph);
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
        int rand = Settings.Random.Next(coll.Count - 1);
        T item = coll.ToArray()[rand];
        coll.Remove(item);
        return item;
    }

    private T ChooseWeightedRandom<T>(ICollection<T> coll, Dictionary<int, int> weight_percents,Func<T,Dictionary<int, int>, int> weighter, int iterations = 3)
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
        return choices.MaxBy((item)=>weighter(item, weight_percents));
    }
}

