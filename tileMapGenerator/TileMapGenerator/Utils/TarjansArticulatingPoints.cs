namespace TileMapGenerator
{
    using QuikGraph;
    using System.Numerics;
    using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
    using Vertex = MapPrimitives.RoomVertex;
    using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
    using DFS = QuikGraph.Algorithms.Search.UndirectedDepthFirstSearchAlgorithm<MapPrimitives.RoomVertex, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
    using System.Collections.Concurrent;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static GraphHelpers.GraphHelpers;
    using System.Threading.Tasks;

    public static class TarjansArticulatingPoints
    {
        public static OrderedHashSet<Vertex> GetNonArticulatingPoints(Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, List<Vector2> validDirections)
        {
            Vertex? root = null;
            int discovery_time = 1;
            OrderedHashSet<Vertex> articulation_points = new OrderedHashSet<Vertex>();
            Dictionary<Vertex, (int, int)> visited_points = new Dictionary<Vertex, (int, int)>();
        
            (graph, backing_dictionary) = CheckForHolesAndPatch(graph, backing_dictionary, validDirections);
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
                    if (((List<Edge>)((Vertex)vert)["children"] ?? new List<Edge>()).Count() >= 2)
                    {
                        articulation_points.Add(vert);
                    }
                }
                else
                {
                    foreach (var edge in ((List<Edge>)((Vertex)vert)["children"]) ?? new List<Edge>())
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

            OrderedHashSet<Vertex> disposable_vertices = new OrderedHashSet<Vertex>(graph.Vertices);
            disposable_vertices.ExceptWith(articulation_points);
            return disposable_vertices;
        }
    

    private static Graph PatchHoles(List<Graph> components, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, List<Vector2> validDirections)
        {
            Graph unified_graph = new Graph(false);
            unified_graph.AddVerticesAndEdgeRange(components[0].Edges);
            foreach (var comp in components.Skip(1))
            {
                Vertex? vertex = null;
                foreach (Vertex potential_vert in comp.Vertices)
                {
                    if (GetAdjacentVertices(backing_dictionary, (Vector2) potential_vert.Weight, validDirections).Count > 0)
                    {
                        foreach (var adj_vertex in GetAdjacentVertices(backing_dictionary, (Vector2) potential_vert.Weight, validDirections))
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
                    var adjacent_vertices = GetAdjacentVertices(backing_dictionary, (Vector2) vertex.Weight, validDirections);
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

        private static (Graph, ConcurrentDictionary<Vector2, Vertex>) CheckForHolesAndPatch(Graph graph, ConcurrentDictionary<Vector2, Vertex> backing_dictionary, List<Vector2> validDirections)
        {
            List<Graph> patching_graphs = graph.GetConnectedComponents();
            if (patching_graphs.Count > 1)
            {
                graph = PatchHoles(patching_graphs, backing_dictionary, validDirections);
            }
            return (graph, backing_dictionary);
        }

        private static IList<Vertex> GetAdjacentVertices(ConcurrentDictionary<Vector2, Vertex> backing_dictionary, Vector2 origin, List<Vector2> validDirections)
        {
            ConcurrentBag<Vertex> adjacent_vertices = new ConcurrentBag<Vertex>();
            Parallel.ForEach(validDirections, dir=>
            {
                if (backing_dictionary.TryGetValue(dir+origin, out Vertex? val))
                {
                    adjacent_vertices.Add(val);
                }
            });
            return adjacent_vertices.ToList();
        }
    }
}