using System.Numerics;
using QuikGraph;
using Grid;
using TileMapGenerator;
using System.Collections.Generic;
using System.Linq;

namespace Primitives
{
    public class ZVertex<Vector2> : IDed where Vector2 : struct
    {
        private int _vertex_id;

        public int ID { get => _vertex_id; set => _vertex_id = value; }
        public HashSet<ZEdge<Vector2>> Edges{get; private set;} = new HashSet<ZEdge<Vector2>>();
        private Dictionary<string, object?> _data = new Dictionary<string, object?>();
        public int Degree{get{return Edges.Count;}}
        public Vector2? Weight {get; internal set;}
        public ZVertex(Vector2 weight)
        {
            Weight = weight;
            _vertex_id = UIDGenerator.GetNextID(Weight!.ToString());
        }

        internal ZVertex()
        {
            _vertex_id = UIDGenerator.GetNextID(" ");
        }

        // public RoomVertex(Dictionary<string, object?> data)
        // {
        //     _data = data;
        //     _vertex_id = UIDGenerator.GetNextID();
        // }

        public void RemoveEdge(ZEdge<Vector2> edge)
        {
            lock (Edges)
            {
                Edges.Remove(edge);
            }
        }

        public ZEdge<Vector2> ConnectToVertex(ZVertex<Vector2> vertex, Vector2 weight)
        {
            ZEdge<Vector2> new_edge = new ZEdge<Vector2>(this, vertex);
            lock (Edges)
            {
                Edges.Add(new_edge);
            }
            lock (vertex.Edges) {
                vertex.Edges.Add(new_edge);
            }
        
            new_edge.Weight = (Vector2) weight;
        
            return new_edge;
        }

        public Dictionary<string, object?> GetData()
        {
            return new Dictionary<string, object?>(_data);
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
            if (obj != null && obj is ZVertex<Vector2> edge)
            {
                return _vertex_id == ((ZVertex<Vector2>) obj)._vertex_id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _vertex_id;
        }
    }
}