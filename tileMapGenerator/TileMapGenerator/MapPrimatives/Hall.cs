namespace MapPrimatives;
using Vertex = RoomVertex<System.Numerics.Vector2>;
using Edge = RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using TileMapGenerator;

public class Hall : IDedThing
{
    public int ID{get; set;}
    public Vector2 Locus{get;}
    public Vertex? Vertex{get;} = null;
    public Edge? Edge{get;} = null;

    internal Hall(Vertex vertex, Vector2 center)
    {
        ID = UIDGenerator.GetNextID("hall" + vertex.ID + center.X + center.Y);
        Vertex = vertex;
        Locus = center;
    }

    internal Hall(Edge edge, Vector2 center)
    {
        ID = UIDGenerator.GetNextID("hall" + edge.ID + center.X + center.Y);
        Edge = edge;
        Locus = center;
    }
}