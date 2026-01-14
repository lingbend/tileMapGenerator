using System.Collections.Immutable;
using System.Numerics;
using QuikGraph;
using BinaryGrid;


namespace TileMapGenerator;

internal interface IDedThing
{
    int ID{get; internal set;}
}

public class RoomEdge<TWeight> : IEdge<RoomVertex<TWeight>>, IDedThing
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

public class RoomVertex<TWeight> : IDedThing
{
    private int _vertex_id;

    public int ID { get => _vertex_id; set => _vertex_id = value; }
    public HashSet<RoomEdge<TWeight>> Edges{get; private set;} = new HashSet<RoomEdge<TWeight>>();
    private Dictionary<string, object?> _data = new Dictionary<string, object?>();
    public int Degree{get{return Edges.Count;}}
    public TWeight? Weight {get; internal set;}

    public RoomVertex(TWeight weight)
    {
        Weight = weight;
        _vertex_id = UIDGenerator.GetNextID(Weight!.ToString());
    }

    internal RoomVertex()
    {
        _vertex_id = UIDGenerator.GetNextID(" ");
    }

    // public RoomVertex(Dictionary<string, object?> data)
    // {
    //     _data = data;
    //     _vertex_id = UIDGenerator.GetNextID();
    // }

    public void RemoveEdge(RoomEdge<TWeight> edge)
    {
        lock (Edges)
        {
            Edges.Remove(edge);
        }
    }

    public RoomEdge<TWeight> ConnectToVertex(RoomVertex<TWeight> vertex, TWeight weight)
    {
        RoomEdge<TWeight> new_edge = new RoomEdge<TWeight>(this, vertex);
        lock (Edges)
        {
            Edges.Add(new_edge);
        }
        lock (vertex.Edges) {
            vertex.Edges.Add(new_edge);
        }
        
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