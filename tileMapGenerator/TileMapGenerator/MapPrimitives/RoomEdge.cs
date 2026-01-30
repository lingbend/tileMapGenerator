using System.Collections.Immutable;
using System.Numerics;
using QuikGraph;
using BinaryGrid;
using TileMapGenerator;

namespace MapPrimitives;

public class RoomEdge<TWeight> : IEdge<RoomVertex<TWeight>>, IDed
{
    public int _edge_id;

    public TWeight? Weight {get; internal set;}
    public RoomVertex<TWeight> Source {get; internal set;}

    public RoomVertex<TWeight> Target {get; internal set;}
    public int ID { get => _edge_id; set => _edge_id = value; }

    public RoomEdge (RoomVertex<TWeight> vertex_1, RoomVertex<TWeight> vertex_2)
    {
        _edge_id = UIDGenerator.GetNextID(vertex_1.ID + vertex_2.ID);
        Source = vertex_1;
        Target = vertex_2;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not null && obj is RoomEdge<TWeight> edge)
        {
            if ((Target == edge.Target && Source == edge.Source) || (Target == edge.Source && Source == edge.Target))
            {
                return true;
            }
            return _edge_id == edge._edge_id;
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return _edge_id;
    }
}

