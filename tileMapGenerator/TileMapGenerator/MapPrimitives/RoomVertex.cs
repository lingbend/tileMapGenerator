using System.Numerics;
using QuikGraph;
using BinaryGrid;
using TileMapGenerator;
using System.Collections.Generic;
using System.Linq;

namespace MapPrimitives
{
    public class RoomVertex<Vector2> : IDed where Vector2 : struct
    {
        private int _vertex_id;

        public int ID { get => _vertex_id; set => _vertex_id = value; }
        public HashSet<RoomEdge<Vector2>> Edges{get; private set;} = new HashSet<RoomEdge<Vector2>>();
        private Dictionary<string, object?> _data = new Dictionary<string, object?>();
        public int Degree{get{return Edges.Count;}}
        public Vector2? Weight {get; internal set;}
        public RoomVertex(Vector2 weight)
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

        public void RemoveEdge(RoomEdge<Vector2> edge)
        {
            lock (Edges)
            {
                Edges.Remove(edge);
            }
        }

        public RoomEdge<Vector2> ConnectToVertex(RoomVertex<Vector2> vertex, Vector2 weight)
        {
            RoomEdge<Vector2> new_edge = new RoomEdge<Vector2>(this, vertex);
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
            if (obj != null && obj is RoomVertex<Vector2> edge)
            {
                return _vertex_id == ((RoomVertex<Vector2>) obj)._vertex_id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _vertex_id;
        }
    }
}