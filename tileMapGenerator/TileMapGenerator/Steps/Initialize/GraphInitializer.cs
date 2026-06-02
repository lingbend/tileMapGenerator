namespace NodeTreeGenerator
{
    using System.Numerics;
    using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
    using Vertex = MapPrimitives.RoomVertex;
    using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
    using System.Collections.Concurrent;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    
    public class GraphInitializer
    {
        public static (Graph, ConcurrentDictionary<Vector2, Vertex>) GenerateFilledGraph(int rows, int cols)
        {
            ConcurrentDictionary<Vector2, Vertex> backing_dictionary = new ConcurrentDictionary<Vector2, Vertex>();
            Graph graph = new Graph(false);
            ConcurrentBag<Edge> edges = new ConcurrentBag<Edge>();

            // initialize all vertices
            Parallel.For(1, rows+1, i=>
            {
                Parallel.For(1, cols+1, j=>
                {
                    Vertex vertex = new Vertex(new Vector2(i, j));
                    backing_dictionary.TryAdd((Vector2) vertex.Weight, vertex);
                });
            });

            var vertical_edging = new Thread(()=>Parallel.For(1, rows+1, i=>
            {
                Parallel.For(1, cols, j=>
                {
                    if (!backing_dictionary.TryGetValue(new Vector2(i, j), out Vertex vert1))
                    {
                        throw new Exception();
                    }
                    if (!backing_dictionary.TryGetValue(new Vector2(i, j+1), out Vertex vert2))
                    {
                        throw new Exception();
                    }
                    Edge edge = vert1.ConnectToVertex(vert2, (Vector2) (vert2.Weight - vert1.Weight));
                    edges.Add(edge);
                });
            }));

            vertical_edging.Start();

            //horizontal edging
            Parallel.For(1, rows, i=>
            {
                Parallel.For(1, cols+1, j=>
                {
                    backing_dictionary.TryGetValue(new Vector2(i, j), out Vertex vert1);
                    backing_dictionary.TryGetValue(new Vector2(i+1, j), out Vertex vert2);
                    Edge edge = vert1!.ConnectToVertex(vert2!, (Vector2) (vert2!.Weight - vert1.Weight)!);
                    edges.Add(edge);
                });
            });

            vertical_edging.Join();

            graph.AddVerticesAndEdgeRange(edges);
            return (graph, backing_dictionary);
        }
    }
}