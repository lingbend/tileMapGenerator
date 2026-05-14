using System.Numerics;
using QuikGraph;
using BitArray2D;
using TileMapGenerator;
using System.Collections.Generic;
using System.Linq;

namespace Primitives
{
    public class VectorVertex<Vector2> : ID where Vector2 : struct
    {
        private int _vertex_id;

        public int ID { get => _vertex_id; set => _vertex_id = value; }
        public HashSet<VectorEdge<Vector2>> Edges{get; private set;} = new HashSet<VectorEdge<Vector2>>();
        private Dictionary<string, object?> _data = new Dictionary<string, object?>();
        public int Degree{get{return Edges.Count;}}
        public Vector2? Weight {get; internal set;}
        public VectorVertex(Vector2 weight)
        {
            Weight = weight;
            _vertex_id = UIDGenerator.GetNextID(Weight!.ToString());
        }

        internal VectorVertex()
        {
            _vertex_id = UIDGenerator.GetNextID(" ");
        }

        // public RoomVertex(Dictionary<string, object?> data)
        // {
        //     _data = data;
        //     _vertex_id = UIDGenerator.GetNextID();
        // }

        public void RemoveEdge(VectorEdge<Vector2> edge)
        {
            lock (Edges)
            {
                Edges.Remove(edge);
            }
        }

        public VectorEdge<Vector2> ConnectToVertex(VectorVertex<Vector2> vertex, Vector2 weight)
        {
            VectorEdge<Vector2> new_edge = new VectorEdge<Vector2>(this, vertex);
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
            if (obj != null && obj is VectorVertex<Vector2> edge)
            {
                return _vertex_id == ((VectorVertex<Vector2>) obj)._vertex_id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _vertex_id;
        }
    }
    public class VectorEdge<Vector2> : IEdge<VectorVertex<Vector2>>, ID where Vector2 : struct
    {
        public int _edge_id;

        public Vector2? Weight {get; internal set;}
        public VectorVertex<Vector2> Source {get; internal set;}

        public VectorVertex<Vector2> Target {get; internal set;}
        public int ID { get => _edge_id; set => _edge_id = value; }

        public VectorEdge (VectorVertex<Vector2> vertex_1, VectorVertex<Vector2> vertex_2)
        {
            _edge_id = UIDGenerator.GetNextID(vertex_1.ID + vertex_2.ID);
            Source = vertex_1;
            Target = vertex_2;
        }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is VectorEdge<Vector2> edge)
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
}