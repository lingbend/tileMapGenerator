namespace TileMapGenerator;

using System.Numerics;
using QuikGraph;

public class RoomGraph
{
    
    UndirectedGraph<int, UndirectedEdge<int>> graph = new UndirectedGraph<int,UndirectedEdge<int>>();
    public RoomGraph()
    {
        BidirectionalMatrixGraph<UndirectedEdge<int>> graphy = new BidirectionalMatrixGraph<UndirectedEdge<int>>(10);
        QuikGraph.TaggedEdge<int, UndirectedEdge<int>> grapy2 = new QuikGraph.TaggedEdge<int,UndirectedEdge<int>>();
    }

}



