// For testing only
#if DEBUG
using Graph = QuikGraph.UndirectedGraph<Primitives.VectorVertex<System.Numerics.Vector2>, Primitives.VectorEdge<System.Numerics.Vector2>>;
using Vertex = Primitives.VectorVertex<System.Numerics.Vector2>;
using Edge = Primitives.VectorEdge<System.Numerics.Vector2>;

using QuikGraph.Graphviz;
using System.Diagnostics;
using System.IO;

namespace GraphExtensions
{
    public static class GraphExt
    {
        public static void PrintToPNG(this Graph graph, string name, bool print_ids = false)
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
    }
}

#endif