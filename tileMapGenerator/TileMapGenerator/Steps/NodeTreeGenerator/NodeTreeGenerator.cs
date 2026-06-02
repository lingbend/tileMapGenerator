namespace NodeTreeGenerator
{
    using TileMapGenerator;
    using static TileMapGenerator.TarjansArticulatingPoints;
    using QuikGraph;
    using System.Numerics;
    using MapPrimitives;
    using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
    using Vertex = MapPrimitives.RoomVertex;
    using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
    using System.Collections.Concurrent;
    #if DEBUG
    using QuikGraph.Graphviz;
    #endif
    using static GraphHelpers.GraphHelpers;
    using static GraphInitializer;
    using System.Diagnostics;
    using System.Collections;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class NodeTreeGenerator
    {
        private Graph _graph = new Graph(false);

        public NodeTreeGeneratorSettings Settings {get; set;} = new NodeTreeGeneratorSettings();

        public NodeTreeGenerator(){}

        internal Graph GenerateNodeTree(int num_rooms)
        {  
            int size = (int) Math.Pow(4.0*(num_rooms*(1.0+(Settings.InitialPaddingPercent/100.0))), .5);
            var (new_graph, backing_dictionary) = GenerateFilledGraph((int) Math.Ceiling(size*Settings.InitialRatio.Y),(int) Math.Ceiling(size*Settings.InitialRatio.X));
            _graph = new_graph;
            return GenerateNodeTreeInner(num_rooms, ref backing_dictionary);
        }

        internal Graph GenerateNodeTree(int num_rooms, Graph graph)
        {
            _graph = graph;
            ConcurrentDictionary<Vector2, Vertex> backing_dictionary = new ConcurrentDictionary<Vector2, Vertex>();
            Parallel.ForEach(_graph.Vertices, vertex =>
            {
                backing_dictionary.TryAdd((Vector2) vertex.Weight, vertex);
            });
            return GenerateNodeTreeInner(num_rooms, ref backing_dictionary);
        }

        private Graph GenerateNodeTreeInner(int num_rooms, ref ConcurrentDictionary<Vector2, Vertex> backing_dictionary)
        {
            if (num_rooms == 1)
            {
                _graph = new Graph();
                Vertex vert = new Vertex(Vector2.One);
                _graph.AddVertex(vert);
                backing_dictionary = new ConcurrentDictionary<Vector2, Vertex>();
                backing_dictionary.TryAdd(Vector2.One, vert);
                return _graph;
            }

            var (new_graph, new_backing_dictionary, holes) = CutVerticesDownTo(_graph, backing_dictionary, (int)(num_rooms * (1.0 + (Settings.InitialPaddingPercent / 100.0))));
            _graph = new_graph;
            backing_dictionary = new_backing_dictionary;
            (_graph, backing_dictionary) = ReworkDegreeDistribution(_graph, backing_dictionary, holes);
            // if (Settings.InitialPaddingPercent != 0)
            // {
                (_graph, backing_dictionary, _) = CutVerticesDownTo(_graph, backing_dictionary, num_rooms);
            // }
            foreach (var processor in Settings.PostProcessors)
            {
                (_graph, backing_dictionary) = processor(_graph, backing_dictionary);
                (_graph, backing_dictionary) = CheckForHolesAndPatch(_graph, backing_dictionary);
            }
            return _graph;
        }

        private (Graph, ConcurrentDictionary<Vector2, Vertex>) ReworkDegreeDistribution(Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, List<Vector2> holes)
        {
            Dictionary<int, int> degree_percents = Settings.degree_percents;
            int min = degree_percents.Keys.Min();
            int max = degree_percents.Keys.Max();

            (graph, backing_dictionary) = ForceEdgesToRange(graph, backing_dictionary, holes, min, max);

            Graph min_span_tree = graph.GetMinSpanTree();
            Graph min_span_tree_copy = graph.GetMinSpanTree();

            OrderedHashSet<Vertex> processed_vertices = new OrderedHashSet<Vertex>();
            OrderedHashSet<Vertex> deleted_vertices = new OrderedHashSet<Vertex>();
            OrderedHashSet<Vertex> new_vertices = new OrderedHashSet<Vertex>();
            OrderedHashSet<Edge> processed_edges = new OrderedHashSet<Edge>(); 
            InnerReworkDegreeDistribution(ref graph, backing_dictionary, min_span_tree, min_span_tree_copy, degree_percents, processed_vertices, processed_edges, deleted_vertices, new_vertices);
            Graph new_vertices_container = new Graph(false);
            new_vertices_container.AddVerticesAndEdgeRange(GetAllEdges(new_vertices));
            InnerReworkDegreeDistribution(ref graph, backing_dictionary, min_span_tree, new_vertices_container, degree_percents, processed_vertices, processed_edges, deleted_vertices, new OrderedHashSet<Vertex>());
            var edge_copy = new List<Edge>(graph.Edges);
            graph.RemoveEdges(new OrderedHashSet<Edge>(edge_copy.Where(e=>!min_span_tree.ContainsEdge(e))));
            return (min_span_tree, backing_dictionary);
        }

        private void InnerReworkDegreeDistribution(ref Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, Graph min_span_tree, Graph min_span_tree_copy, Dictionary<int, int> degree_percents, OrderedHashSet<Vertex> processed_vertices, OrderedHashSet<RoomEdge<Vector2>> processed_edges, OrderedHashSet<Vertex> deleted_vertices, OrderedHashSet<Vertex> new_vertices)
        {
            foreach ((Vertex next_vertex, int target_degree) in GetNextVertexDegreePair(min_span_tree_copy, new ConcurrentDictionary<Vector2, Vertex>(backing_dictionary), degree_percents))
            {
                processed_vertices.Add(next_vertex);
                if (deleted_vertices.Contains(next_vertex))
                {
                    continue;
                }

                int degree = 0;
                TryGetExistingEdges(min_span_tree, next_vertex, target_degree, ref degree);

                TryTransplantEdges(ref graph, backing_dictionary, min_span_tree, processed_vertices, processed_edges, next_vertex, target_degree, ref degree);

                var (deleted, new_verts) = TryTransplantVertices(ref graph, backing_dictionary, min_span_tree, processed_vertices, processed_edges, next_vertex, target_degree, ref degree);

                deleted_vertices.UnionWith(deleted);
                new_vertices.UnionWith(new_verts);
                processed_edges.UnionWith(next_vertex.Edges);
            }
        }

        private void TryTransplantEdges(ref Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, Graph min_span_tree, OrderedHashSet<Vertex> processed_vertices, OrderedHashSet<RoomEdge<Vector2>> processed_edges, Vertex next_vertex, int target_degree, ref int degree)
        {

            while (degree < target_degree)
            {
                var nearby_vertices = new OrderedHashSet<Vertex>(GetAdjacentVertices(backing_dictionary, (Vector2) next_vertex.Weight));
                var connected_vertices = new OrderedHashSet<Vertex>(graph.AdjacentVertices(next_vertex));

                if (nearby_vertices.IsSubsetOf(connected_vertices))
                {
                    break;
                }

                OrderedHashSet<Vertex> sacrificial_vertices = GetNonArticulatingPoints(graph, backing_dictionary, Settings.ValidDirections);
                sacrificial_vertices.ExceptWith(processed_vertices);

                if (sacrificial_vertices.Count != 0)
                {
                    var sacrificial_edges = GetAllEdges(sacrificial_vertices);
                    var sacrificial_edge = sacrificial_edges.ChooseRandom(Settings.Random);

                    if (sacrificial_edge == null)
                    {
                        break;
                    }

                    while (processed_edges.Contains(sacrificial_edge) || sacrificial_edge.Target.Degree == 1 || sacrificial_edge.Source.Degree == 1 || min_span_tree.ContainsEdge(sacrificial_edge))
                    {
                        sacrificial_edge = sacrificial_edges.ChooseRandom(Settings.Random);

                        if (sacrificial_edge == null || sacrificial_edges.Count == 0)
                        {
                            break;
                        }
                    }

                    if (sacrificial_edge != null && !processed_edges.Contains(sacrificial_edge) && !min_span_tree.ContainsEdge(sacrificial_edge))
                    {
                        graph.RemoveEdge(sacrificial_edge);
                    }
                }

                foreach (var vertex in nearby_vertices)
                {
                    if (!connected_vertices.Contains(vertex))
                    {
                        min_span_tree.AddEdge(next_vertex.ConnectToVertex(vertex, (Vector2) (next_vertex.Weight - vertex.Weight)));
                        degree++;
                        break;
                    }
                }
            }
        }

        private (OrderedHashSet<Vertex>, OrderedHashSet<Vertex>) TryTransplantVertices(ref Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, Graph min_span_tree, OrderedHashSet<Vertex> processed_vertices, OrderedHashSet<RoomEdge<Vector2>> processed_edges, Vertex next_vertex, int target_degree, ref int degree)
        {
            OrderedHashSet<Vertex> deleted_vertices = new OrderedHashSet<Vertex>();
            OrderedHashSet<Vertex> new_vertices = new OrderedHashSet<Vertex>();
            while (degree < target_degree)
            {
                OrderedHashSet<Vertex> sacrificial_vertices = GetNonArticulatingPoints(graph, backing_dictionary, Settings.ValidDirections);

                sacrificial_vertices.ExceptWith(processed_vertices);
                sacrificial_vertices.ExceptWith(GetAllVertices(processed_edges));

                if (sacrificial_vertices.Count != 0)
                {
                    Vertex sacrificial_vertex = sacrificial_vertices.ChooseRandom(Settings.Random);
                    deleted_vertices.Add(sacrificial_vertex);
                    graph.RemoveVertex(sacrificial_vertex);

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
                        graph.RemoveEdge(edge);
                    }
                
                    backing_dictionary.TryRemove((Vector2) sacrificial_vertex.Weight, out _);
                    Vector2 direction = GetAvailableDirections(next_vertex, backing_dictionary).ChooseRandom(Settings.Random);
                    if (direction == Vector2.Zero)
                    {
                        continue;
                    }
                    Vertex replacement_vertex = new Vertex((Vector2) next_vertex.Weight + direction);
                    new_vertices.Add(replacement_vertex);
                    Edge connection = next_vertex.ConnectToVertex(replacement_vertex, direction);
                    graph.AddVerticesAndEdge(connection);
                    min_span_tree.AddVerticesAndEdge(connection);
                    backing_dictionary.TryAdd((Vector2) replacement_vertex.Weight, replacement_vertex);
                    processed_edges.Add(connection);
                    degree++;
                }
                else
                {
                    break;
                }
            }  
            return (deleted_vertices, new_vertices);  
        }

        internal IList<Vector2> GetAvailableDirections(Vertex node, ConcurrentDictionary<Vector2, Vertex> backing_dictionary)
        {
            ConcurrentDictionary<Vector2, bool> available_directions = new ConcurrentDictionary<Vector2, bool>
            (Settings.ValidDirections.Distinct().Select((vect)=>new KeyValuePair<Vector2, bool>(vect, true)));
        
            Parallel.ForEach(node.Edges, edge =>
            {
                if (edge.Source == node)
                {
                    available_directions.TryRemove((Vector2) edge.Weight, out _);
                }
                else
                {
                    available_directions.TryRemove((Vector2) (-edge.Weight), out _);
                }
            });
                
            Vector2 relative_loc = (Vector2) node.Weight;

            Parallel.ForEach(available_directions.Keys, direction=>{
            {
                if (backing_dictionary.ContainsKey(direction + relative_loc))
                {
                    available_directions.TryRemove(direction, out _);
                }
            }
            });

            return new List<Vector2>(available_directions.Keys);
        }

        private ISet<Vertex> GetAllVertices(IEnumerable<Edge> edges)
        {
            ConcurrentDictionary<Vertex, bool> vertices = new ConcurrentDictionary<Vertex, bool>();
            Parallel.ForEach(edges, edge=>
            {
                vertices.TryAdd(edge.Source, true);
                vertices.TryAdd(edge.Target, true);
            });
            return new OrderedHashSet<Vertex>(vertices.Keys);
        }

        private void TryGetExistingEdges(Graph min_span_tree, Vertex next_vertex, int target_degree, ref int degree)
        {
            var edges = min_span_tree.Edges;
            int degree_adder = 0;
            Parallel.ForEach(edges, edge =>
            {
                if (edges.Contains(edge))
                {
                    Interlocked.Increment(ref degree_adder);
                }
            });
            degree += degree_adder;

            foreach (var edge in next_vertex.Edges)
            {
                if (degree == target_degree)
                {
                    break;
                }
                if (!edges.Contains(edge))
                {
                    min_span_tree.AddVerticesAndEdge(edge);
                    degree++;
                }
            }
        }

        private IEnumerable<(Vertex, int)> GetNextVertexDegreePair(Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, Dictionary<int, int> degree_percents)
        {
            return Settings.Shaper(graph, backing_dictionary, degree_percents);
        }   

        private (Graph, ConcurrentDictionary<Vector2, Vertex>) ForceEdgesToRange(Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, List<Vector2> holes, int min, int max)
        {
            List<(Vertex, int)> unforced_vertices = new List<(Vertex, int)>();
            foreach (var (vertex, degree) in GetNextVertexDegreePair(graph, backing_dictionary, Settings.degree_percents))
            {
                var disposable_vertices = GetNonArticulatingPoints(graph, backing_dictionary, Settings.ValidDirections);
                if (disposable_vertices.Contains(vertex))
                {
                    while (graph.AdjacentDegree(vertex) > min)
                    {
                        var edge = vertex.Edges.ChooseRandom(Settings.Random);
                        graph.RemoveEdge(edge);
                    }

                    if (graph.AdjacentDegree(vertex) < max)
                    {
                        List<Vector2> current_directions = new List<Vector2>();
                        foreach (var edge in vertex.Edges)
                        {
                            current_directions.Add((Vector2) edge.Weight);
                        }
                        HashSet<Vector2> possible_directions = new HashSet<Vector2>(Settings.ValidDirections);
                        possible_directions.ExceptWith(current_directions);
                        List<Vector2> possible_directions_list = new List<Vector2>(possible_directions);
                        while (vertex.Degree < min)
                        {
                            if (possible_directions_list.Count == 0)
                            {
                                unforced_vertices.Add((vertex, degree));
                                break;
                            }

                            var chosen_direction = possible_directions_list.ChooseRandom(Settings.Random);
                            if (backing_dictionary.TryGetValue(chosen_direction, out Vertex result))
                            {
                                graph.AddEdge(vertex.ConnectToVertex(backing_dictionary[(Vector2) result.Weight], (Vector2) (backing_dictionary[((Vector2) result.Weight)].Weight-vertex.Weight)));
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
                    graph.RemoveEdges(value?.Edges ?? new HashSet<RoomEdge<Vector2>>());
                    if (value != null)
                    {
                        graph.RemoveVertex(backing_dictionary[hole_full]);
                        backing_dictionary.TryRemove(hole_full, out _);
                    }
                
                    Vertex new_vertex = new Vertex(hole_full);
                    backing_dictionary.TryAdd(hole_full, new_vertex);
                    graph.AddVertex(new_vertex);
                    var adjacent_vertices = GetAdjacentVertices(backing_dictionary, hole_full);
                    while (new_vertex.Degree != target_degree)
                    {
                        var choice = adjacent_vertices.ChooseRandom(Settings.Random);
                        graph.AddEdge(new_vertex.ConnectToVertex(choice, (Vector2) (choice.Weight - new_vertex.Weight)));
                    }
                }
                if (graph.GetConnectedComponents().Count > 1)
                {
                    throw new Exception();
                }
            }
            return (graph, backing_dictionary);
        }

        private (Vector2?, bool) GetHoleOfDegreeOrGreater(ConcurrentDictionary<Vector2, Vertex> backing_dictionary, List<Vector2> holes, int degree_min)
        {
            string locker = string.Empty;
            Span<Vector2> holes_copy = new Span<Vector2>(holes.ToArray());
            Settings.Random.Shuffle(holes_copy);
            Vector2? found_hole = null;
            Parallel.ForEach(holes_copy.ToArray(), (hole, state)=>
            {
                if (GetAdjacentVertices(backing_dictionary, hole).Count >= degree_min)
                {
                    lock (locker)
                    {
                        found_hole = hole;
                        state.Stop();
                    }
                }
            });

            return (found_hole, found_hole != null);
        }

        private List<Vertex> GetAdjacentVertices(ConcurrentDictionary<Vector2, Vertex> backing_dictionary, Vector2 origin)
        {
            ConcurrentBag<Vertex> adjacent_vertices = new ConcurrentBag<Vertex>();
            Parallel.ForEach(Settings.ValidDirections, dir=>
            {
                if (backing_dictionary.TryGetValue(dir+origin, out Vertex? val))
                {
                    adjacent_vertices.Add(val);
                }
            });
            return adjacent_vertices.ToList();
        }

        internal (Graph, ConcurrentDictionary<Vector2, Vertex>, List<Vector2>) CutVerticesDownTo(Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, int target_number)
        {
            List<Vector2> holes = new List<Vector2>();
            while (graph.VertexCount > target_number)
            {
                OrderedHashSet<Vertex> disposable_vertices = GetNonArticulatingPoints(graph, backing_dictionary, Settings.ValidDirections);
                Vertex choice = ChooseWeightedRandom(disposable_vertices, Settings.degree_percents, Settings.WeightedVertexRemover, ((target_number*Settings.PruningSelectivityMultiplier)/5)+2);

                graph.RemoveEdges(new OrderedHashSet<Edge>(choice.Edges));
                graph.RemoveVertex(choice);
                backing_dictionary.TryRemove((Vector2) choice.Weight, out _);
                holes.Add((Vector2) choice.Weight);

            }
            return (graph, backing_dictionary, holes);
        }

        private Graph PatchHoles(List<Graph> components, ConcurrentDictionary<Vector2, Vertex> backing_dictionary)
        {
            Graph unified_graph = new Graph(false);
            unified_graph.AddVerticesAndEdgeRange(components[0].Edges);
            foreach (var comp in components.Skip(1))
            {
                Vertex? vertex = null;
                foreach (Vertex potential_vert in comp.Vertices)
                {
                    if (GetAdjacentVertices(backing_dictionary, (Vector2) potential_vert.Weight).Count > 0)
                    {
                        foreach (var adj_vertex in GetAdjacentVertices(backing_dictionary, (Vector2) potential_vert.Weight))
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
                            if (Math.Abs(((Vector2) (vertex3.Weight - vertex4.Weight)).Length()) <= 1)
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
                        return Math.Abs(((Vector2) (tuple.Item1.Weight - tuple.Item2.Weight)).Length());
                    }));
                    unified_graph.AddVerticesAndEdge(alternative_pairs[0].Item1.ConnectToVertex(alternative_pairs[0].Item2, (Vector2) (alternative_pairs[0].Item2.Weight - alternative_pairs[0].Item1.Weight)));
                    unified_graph.AddVerticesAndEdgeRange(comp.Edges);
                
                }
                else
                {
                    var adjacent_vertices = GetAdjacentVertices(backing_dictionary, (Vector2) vertex.Weight);
                    foreach (var adj_vertex in adjacent_vertices)
                    {
                        if (unified_graph.ContainsVertex(adj_vertex))
                        {
                            unified_graph.AddVerticesAndEdge(vertex.ConnectToVertex(adj_vertex, (Vector2) (adj_vertex.Weight - vertex.Weight)));
                            unified_graph.AddVerticesAndEdgeRange(comp.Edges);
                        }
                    }
                    unified_graph.AddVerticesAndEdgeRange(comp.Edges);
                }
            }
            return unified_graph;
        }

        private (Graph, ConcurrentDictionary<Vector2, Vertex>) CheckForHolesAndPatch(Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary)
        {
            List<Graph> patching_graphs = graph.GetConnectedComponents();
            if (patching_graphs.Count > 1)
            {
                graph = PatchHoles(patching_graphs, backing_dictionary);
            }
            return (graph, backing_dictionary);
        }

        private T ChooseWeightedRandom<T>(ICollection<T> coll, Dictionary<int, int> weight_percents,Func<T,Dictionary<int, int>, int> weighter, int iterations = 3) where T : IDed
        {
            List<T> collection_copy = new List<T>(coll.OrderBy(v=>v.ID));
            List<T> choices = new List<T>();
            for (int i = 0; i < iterations; i++)
            {
                if (collection_copy.Count == 0)
                {
                    break;
                }
                choices.Add(collection_copy.ChooseRandom(Settings.Random));
            }
            choices.Sort((c1, c2)=>weighter(c1, weight_percents) - weighter(c2, weight_percents));
            return choices.First();
        }

        #if DEBUG
        private void PrintToPNG(Graph graph, string name)
        {
            var visualizer = new GraphvizAlgorithm<Vertex, Edge>(graph);
            visualizer.FormatVertex += (_, args) =>
            {
                args.VertexFormat.Position = new QuikGraph.Graphviz.Dot.GraphvizPoint((int) args.Vertex.Weight?.X * 72, (int) args.Vertex.Weight?.Y * 72);
            };
            string file = visualizer.Generate()[..^1] + "layout=neato;\n}";
            File.WriteAllText($"../../{name}.dot", file);
            using var process = Process.Start("dot", $"-Tpng -n ../../{name}.dot -o ../../{name}.png");
            process.WaitForExit();
        }
        #endif
    }
}
