using System.Collections.Immutable;
using System.Numerics;
using QuikGraph;
using BinaryGrid;


namespace TileMapGenerator;

public class RoomEdge<TWeight> : IEdge<RoomVertex<TWeight>>
{
    public int _edge_id;
    public TWeight? Weight {get; internal set;}
    public RoomVertex<TWeight> Source {get; internal set;}

    public RoomVertex<TWeight> Target {get; internal set;}

    public RoomEdge (RoomVertex<TWeight> vertex_1, RoomVertex<TWeight> vertex_2)
    {
        _edge_id = UIDGenerator.GetNextID();
        Source = vertex_1;
        Target = vertex_2;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not null && obj is RoomEdge<TWeight> edge)
        {
            return _edge_id == ((RoomEdge<TWeight>) obj)._edge_id;
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return _edge_id;
    }
}

public class RoomVertex<TWeight>
{
    private int _vertex_id;
    public HashSet<RoomEdge<TWeight>> Edges{get; private set;} = new HashSet<RoomEdge<TWeight>>();
    private Dictionary<string, object?> _data = new Dictionary<string, object?>();
    public int Degree{get{return Edges.Count;}}
    public TWeight? Weight {get; internal set;}
    public bool Center{get; set;} = false;
    public UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>>? InnerGraph{get; set;} = null;

    public RoomVertex(TWeight weight)
    {
        _vertex_id = UIDGenerator.GetNextID();
        Weight = weight;
    }

    public RoomVertex()
    {
        _vertex_id = UIDGenerator.GetNextID();
    }

    public RoomVertex(Dictionary<string, object?> data)
    {
        _data = data;
        _vertex_id = UIDGenerator.GetNextID();
    }

    public bool RemoveEdge(RoomEdge<TWeight> edge)
    {
        return Edges.Remove(edge);
    }

    public RoomEdge<TWeight> ConnectToVertex(RoomVertex<TWeight> vertex, TWeight weight)
    {
        RoomEdge<TWeight> new_edge = new RoomEdge<TWeight>(this, vertex);
        Edges.Add(new_edge);
        vertex.Edges.Add(new_edge);
        new_edge.Weight = (TWeight) weight;
        
        return new_edge;
    }

    public ImmutableDictionary<string, object?> GetData()
    {
        return _data.ToImmutableDictionary();
    }

    public object? this[string key]
    {
        get {
            if (_data.TryGetValue(key, out object? value))
            {
                return value;
            }
            else
            {
                return null;
            }
              }
        set
        {
            _data[key] = value;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not null && obj is RoomVertex<TWeight> edge)
        {
            return _vertex_id == ((RoomVertex<TWeight>) obj)._vertex_id;
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return _vertex_id;
    }
}