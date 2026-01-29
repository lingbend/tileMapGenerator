namespace CellularRoomGrower;
using Vertex = RoomAndEdges.RoomVertex<System.Numerics.Vector2>;
using Edge = RoomAndEdges.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;

public class Hall
{
    public Vector2 Locus{get;}
    public Vertex? Vertex{get;} = null;
    public Edge? Edge{get;} = null;

    internal Hall(Vertex vertex, Vector2 center)
    {
        Vertex = vertex;
        Locus = center;
    }

    internal Hall(Edge edge, Vector2 center)
    {
        Edge = edge;
        Locus = center;
    }
}