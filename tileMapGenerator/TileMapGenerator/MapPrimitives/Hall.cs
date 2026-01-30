namespace MapPrimitives;
using Vertex = RoomVertex<System.Numerics.Vector2>;
using Edge = RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using TileMapGenerator;

public class Hall : IDed
{
    public int ID{get; set;}
    public Vector2 Locus{get;}
    public Edge Edge{get;}

    internal Hall(Edge edge, Vector2 center)
    {
        ID = UIDGenerator.GetNextID("hall" + edge.ID + center.X + center.Y);
        Edge = edge;
        Locus = center;
    }
}