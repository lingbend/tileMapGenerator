using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Dynamic;
using System.Numerics;
using QuikGraph;

namespace TileMapGenerator;

public class RoomGraph
{
    
    // UndirectedGraph<int, UndirectedEdge<int>> graph = new UndirectedGraph<int,UndirectedEdge<int>>();
    // public RoomGraph()
    // {
    //     BidirectionalMatrixGraph<UndirectedEdge<int>> graphy = new BidirectionalMatrixGraph<UndirectedEdge<int>>(10);
    //     QuikGraph.TaggedEdge<int, UndirectedEdge<int>> grapy2 = new QuikGraph.TaggedEdge<int,UndirectedEdge<int>>();
    // }

    public int Degree { get; private set;}
    public int VertexCount{ get; private set;}
    public int EdgeCount { get; private set;}
    public int MaxDepth { get; private set;}
    public bool Connected { get; private set;}
    private UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>> _graph = new UndirectedGraph<RoomVertex<Vector2>, RoomEdge<Vector2>>();
    // public RoomVertex<Vector2> AddVertex(Dictionary<string, object> data)
    // {
    //     RoomVertex<Vector2> vertex = new RoomVertex<Vector2>(data);
    //     _graph.AddVertex(vertex);
    //     return vertex;
    // }

    public bool AddVertexIf(Dictionary<string, object> data, Func<IEnumerable<Dictionary<string, object>>, bool> condition, out Dictionary<string, object> new_vertex)
    {
        
        throw new NotImplementedException();
    }

    public void RemoveVertex(Dictionary<string, object> data){
        throw new NotImplementedException();
    }

    public void RemoveVertex(int id){
        throw new NotImplementedException();
    }

    public bool RemoveVerticesIf(Func<IEnumerable<Dictionary<string, object>>, bool> condition, out IEnumerable<Dictionary<string, object>> removed_vertices)
    {
        throw new NotImplementedException();
    }

    public void AddEdge(Dictionary<string, object> vertex1, Dictionary<string, object> vertex2)
    {
        throw new NotImplementedException();
    }

    public void AddEdge(int vertex_id_1, int vertex_id_2)
    {
        throw new NotImplementedException();
    }

    public bool AddEdgeIf(Dictionary<string, object> vertex1, Dictionary<string, object> vertex2, Func<IEnumerable<Dictionary<string, object>>, bool> condition)
    {
        throw new NotImplementedException();
    }

    public bool AddEdgeIf(int vertex_id_1, int vertex_id_2, Func<IEnumerable<Dictionary<string, object>>, bool> condition)
    {
        throw new NotImplementedException();
    }

    public void RemoveEdge(int edge_id)
    {
        throw new NotImplementedException();
    }

    public void RemoveEdge(int vertex_id_1, int vertex_id_2)
    {
        throw new NotImplementedException();
    }


    public void RemoveEdge(Dictionary<string, object> vertex1, Dictionary<string, object> vertex2)
    {
        throw new NotImplementedException();
    }

    public bool RemoveEdgesIf(Func<IEnumerable<Dictionary<string, object>>, bool> condition, out IEnumerable<Dictionary<string, object>> removed_edges)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, object>[][] ToMatrix()
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, object> GetVertexData(int vertex_id)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, object> GetVertexData(Vector2 absolute_location)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, object> GetVertexData(int start_vertex_id, Vector2 relative_location)
    {
        throw new NotImplementedException();
    }

    public bool HasVertex(int vertex_id)
    {
        throw new NotImplementedException();
    }

    public bool HasVertex(Vector2 absolute_location)
    {
        throw new NotImplementedException();
    }

    public bool HasVertex(int start_vertex_id, Vector2 relative_location)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Dictionary<int, (Vector2, Dictionary<string, object>)>> GetAllVertices()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Dictionary<string, object>> GetAllVerticesData()
    {
        throw new NotImplementedException();
    }

    public void GetAllEdges()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<RoomGraph> GetConnectedComponents()
    {
        throw new NotImplementedException();
    }

    public void GetAdjacentVertices(int vertex_id)
    {
        throw new NotImplementedException();
    }

    public void GetAdjacentVertices(Dictionary<string, object> vdata)
    {
        throw new NotImplementedException();
    }

    public void GetEndpointVertices(int edge_id)
    {
        throw new NotImplementedException();
    }

    public void GetLeaves()
    {
        throw new NotImplementedException();
    }
}

public class RoomEdge<TWeight> : IEdge<RoomVertex<TWeight>>
{
    private int _edge_id;
    private TWeight? Weight {get;}
    public RoomVertex<TWeight> Source {get;}

    public RoomVertex<TWeight> Target {get;}

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
    private List<RoomEdge<TWeight>> _edges = new List<RoomEdge<TWeight>>();
    private Dictionary<string, object?> _data = new Dictionary<string, object?>();

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
        return _edges.Remove(edge);
    }

    public RoomEdge<TWeight> ConnectToVertex(RoomVertex<TWeight> vertex)
    {
        RoomEdge<TWeight> new_edge = new RoomEdge<TWeight>(this, vertex);
        _edges.Add(new_edge);
        vertex._edges.Add(new_edge);
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