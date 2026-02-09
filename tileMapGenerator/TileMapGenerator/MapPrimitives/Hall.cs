namespace MapPrimitives;
using Vertex = RoomVertex<System.Numerics.Vector2>;
using Edge = RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using TileMapGenerator;

public class Hall : IDed
{
    public int ID{get; set;}
    public Vector2 Locus{get;}
    public Vector2 SourceLocus{get;}
    public Vector2 TargetLocus{get;}
    public Edge Edge{get;}
    public HashSet<Vector2> InsidePoints{get; set;} = new();
    public HashSet<Vector2> WallPoints{get; set;} = new();

    internal Hall(Edge edge, Vector2 center, Vector2 source_locus, Vector2 target_locus)
    {
        ID = UIDGenerator.GetNextID("hall" + edge.ID + center.X + center.Y);
        Edge = edge;
        Locus = center;
        SourceLocus = source_locus;
        TargetLocus = target_locus;
    }
}