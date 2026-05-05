using System.Numerics;
using QuikGraph;
using BinaryGrid;
using TileMapGenerator;
using System;

namespace MapPrimitives
{
    public class RoomEdge<Vector2> : IEdge<RoomVertex<Vector2>>, IDed where Vector2 : struct
    {
        public int _edge_id;

        public Vector2? Weight {get; internal set;}
        public RoomVertex<Vector2> Source {get; internal set;}

        public RoomVertex<Vector2> Target {get; internal set;}
        public int ID { get => _edge_id; set => _edge_id = value; }

        public RoomEdge (RoomVertex<Vector2> vertex_1, RoomVertex<Vector2> vertex_2)
        {
            _edge_id = UIDGenerator.GetNextID(vertex_1.ID + vertex_2.ID);
            Source = vertex_1;
            Target = vertex_2;
        }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is RoomEdge<Vector2> edge)
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
