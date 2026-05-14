namespace HallMaker
{
    using BitArray2D;
    using Graph = QuikGraph.UndirectedGraph<Primitives.VectorVertex<System.Numerics.Vector2>, Primitives.VectorEdge<System.Numerics.Vector2>>;
    using Vertex = Primitives.VectorVertex<System.Numerics.Vector2>;
    using Edge = Primitives.VectorEdge<System.Numerics.Vector2>;
    using System.Numerics;
    using System.Collections.Concurrent;
    using Vector2Extensions;
    using Primitives;
    using GoRogueWrapper;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // TODO: Refactor to internal
    public class HallMaker
    {
        public HallMaker(){}

        public (Graph, BitArray2D, IEnumerable<Zone>, IEnumerable<Tunnel>) GenerateHalls(Graph graph, BitArray2D grid, IEnumerable<Zone> rooms, IEnumerable<Tunnel> halls)
        {
            var room_points = GetRoomInnerPoints(rooms);
            foreach (Tunnel hall in halls)
            {
                grid = AddHallToAll(grid, room_points, hall);
            }

            return (graph, grid, rooms, halls);
        }

        public BitArray2D GenerateOnlyHalls(in Graph graph, in BitArray2D grid, in IEnumerable<Zone> rooms, in IEnumerable<Tunnel> halls)
        {
            var out_grid = new BitArray2D(grid.NRows, grid.NCols);
            var room_points = GetRoomInnerPoints(rooms);
            foreach (Tunnel hall in halls)
            {
                out_grid.CombineGrids(new BitArray2D[]{AddHallAlone(grid, room_points, hall)});

            }
            return out_grid;
        }

        private BitArray2D AddHallAlone(in BitArray2D grid, in IEnumerable<Vector2> room_points, in Tunnel hall)
        {
            BitArray2D temp_grid = new BitArray2D(grid.NRows, grid.NCols);
            (temp_grid, _, _) = AddHallInner(room_points, hall, temp_grid);

            return temp_grid;
        }

        private BitArray2D AddHallToAll(BitArray2D grid, IEnumerable<Vector2> room_points, Tunnel hall)
        {
            var (new_grid, inner_points, wall_points) = AddHallInner(room_points, hall, grid);
            grid = new_grid;
            hall.InsidePoints = new HashSet<Vector2>(inner_points);
            hall.WallPoints = new HashSet<Vector2>(wall_points);

            return grid;
        }

        private (BitArray2D, IEnumerable<Vector2>, IEnumerable<Vector2>) AddHallInner(IEnumerable<Vector2> room_points, Tunnel hall, BitArray2D grid)
        {
            var (inner_points, wall_points) = GenerateHall(hall, room_points);
            foreach (var wall_point in wall_points.Except(room_points))
            {
                if (InBounds(wall_point, grid))
                {
                    grid.SetCell(wall_point.Reverse(), 1u);
                }

            }
            foreach (var inner_point in inner_points)
            {
                grid.SetCell(inner_point.Reverse(), 0u);
            }
        

            return (grid, inner_points, wall_points);
        }

        private IEnumerable<Vector2> GetRoomInnerPoints(IEnumerable<Zone> rooms)
        {
            ConcurrentBag<IEnumerable<Vector2>> points = new ConcurrentBag<IEnumerable<Vector2>>();
            Parallel.ForEach(rooms, room =>
            {
                points.Add(room.GetInsidePoints());
            });
            return new HashSet<Vector2>(points.SelectMany(i=>i));
        }

        private (IEnumerable<Vector2>, IEnumerable<Vector2>) GenerateHall(Tunnel hall, IEnumerable<Vector2> room_inner)
        {
            HashSet<Vector2> inner_points = new HashSet<Vector2>(GenerateInsideHall(hall.Locus, hall.SourceLocus));
            inner_points.UnionWith(GenerateInsideHall(hall.Locus, hall.TargetLocus));
            var wall_points = GenerateHallWalls(inner_points);
            return (inner_points, wall_points);
        }

        private IEnumerable<Vector2> GenerateInsideHall(Vector2 source, Vector2 target)
        {
            return GoRogue.GetSimpleDirectHall(source, target);
        }

        private IEnumerable<Vector2> GenerateHallWalls(IEnumerable<Vector2> inner_points)
        {
            ConcurrentDictionary<Vector2, bool> wall_points = new ConcurrentDictionary<Vector2, bool>();
            Parallel.ForEach(inner_points, point =>
            {
                foreach (var neighbor in point.GetNeighbors())
                {
                    wall_points.TryAdd(neighbor, true);
                }
            });
            return wall_points.Keys;
        }

        private bool InBounds(Vector2 location, BitArray2D grid)
        {
            return location.X > 0 && location.Y > 0 && location.X <= grid.NCols && location.Y <= grid.NRows;
        }
    }
}