#if DEBUG
using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
using Vertex = MapPrimitives.RoomVertex;
using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;

using QuikGraph.Graphviz;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Numerics;

namespace GraphHelpers
{
    public static class GraphHelpers
    {
        #if DEBUG
        internal static void PrintToPNG(this Graph graph, string name, bool print_ids = false)
        {
        
            string file;
            if (print_ids)
            {
                var visualizer = new GraphvizAlgorithm<Vertex, Edge>(graph);
                visualizer.FormatVertex += (_, args) =>
                {
                    args.VertexFormat.Position = new QuikGraph.Graphviz.Dot.GraphvizPoint((int) args.Vertex.Weight?.X! * 72, (int) args.Vertex.Weight?.Y! * 72);
                    args.VertexFormat.Label = args.Vertex.ID.ToString();
                };
                file = visualizer.Generate()[..^1] + "layout=neato;\n}";
            }
            else
            {
                file = graph.ToGraphviz();
            }
        
        
            File.WriteAllText($"../../{name}.dot", file);
            using var process = Process.Start("dot", $"-Tpng -n ../../{name}.dot -o ../../{name}.png");
            process.WaitForExit();
        }
        #endif

        internal static Graph RemoveEdges(this Graph graph, ISet<Edge> edges)
        {
            lock (graph)
            {
                graph.RemoveEdges(edges);
            }
            Parallel.ForEach(edges, edge =>
            {
                edge.Source.RemoveEdge(edge);
                edge.Target.RemoveEdge(edge);
            });
            return graph;
        }

        internal static Graph RemoveEdge(this Graph graph, Edge edge)
        {
            lock (graph)
            {
                graph.RemoveEdge(edge);
            }
            edge.Source.RemoveEdge(edge);
            edge.Target.RemoveEdge(edge);
            return graph;
        }

        internal static IList<Edge> GetAllEdges(IEnumerable<Vertex> vertices)
        {
        
            ConcurrentBag<Edge> edges = new ConcurrentBag<Edge>();
            Parallel.ForEach(vertices, vertex =>
            {
                Parallel.ForEach(vertex.Edges, edge =>
                {
                    edges.Add(edge);
                });
            });
            return new List<Edge>(edges);
        }
        internal static Graph GetMinSpanTree(this Graph graph)
        {
            Graph min_span_tree = new Graph(false);
            var tree = new QuikGraph.Algorithms.MinimumSpanningTree.PrimMinimumSpanningTreeAlgorithm<Vertex, Edge>(graph, (Edge edge)=>(double) System.Math.Abs(((Vector2) (edge.Source.Weight-edge.Target.Weight)).Length()));
            tree.TreeEdge += ((edge) =>
            {
                min_span_tree.AddVerticesAndEdge(edge);
            });
            tree.Compute();
            return min_span_tree;
        } 

        internal static List<Graph> GetConnectedComponents(this Graph graph)
        {
            var test_connect = new QuikGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<Vertex, Edge>(graph);
            test_connect.Compute();
            if (test_connect.ComponentCount > 1)
            {
                List<Graph> components = new List<Graph>();
                foreach (var (vertex, component) in test_connect.Components)
                {
                    if (components.Count < component + 1)
                    {
                        components.Add(new Graph(false));
                    }
                    components[component].AddVerticesAndEdgeRange(vertex.Edges);
                    components[component].AddVertex(vertex);
                }
                return components;
            }
            return new List<Graph>(){graph};
        }
    }
}

#endif