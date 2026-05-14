namespace HallMaker
{
    using Grid;
    using Graph = QuikGraph.UndirectedGraph<Primitives.ZVertex<System.Numerics.Vector2>, Primitives.ZEdge<System.Numerics.Vector2>>;
    using Vertex = Primitives.ZVertex<System.Numerics.Vector2>;
    using Edge = Primitives.ZEdge<System.Numerics.Vector2>;
    using System.Numerics;
    using System.Collections.Concurrent;
    using Vector2Extensions;
    using Primitives;
    using GoRogueWrapper;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class HallMaker
    {
        public HallMaker(){}

        public (Graph, Grid, IEnumerable<Zone>, IEnumerable<Hall>) GenerateHalls(Graph graph, Grid grid, IEnumerable<Zone> rooms, IEnumerable<Hall> halls)
        {
            var room_points = GetRoomInnerPoints(rooms);
            foreach (Hall hall in halls)
            {
                grid = AddHallToAll(grid, room_points, hall);
            }

            return (graph, grid, rooms, halls);
        }

        public Grid GenerateOnlyHalls(in Graph graph, in Grid grid, in IEnumerable<Zone> rooms, in IEnumerable<Hall> halls)
        {
            var out_grid = new Grid(grid.NRows, grid.NCols);
            var room_points = GetRoomInnerPoints(rooms);
            foreach (Hall hall in halls)
            {
                out_grid.CombineGrids(new Grid[]{AddHallAlone(grid, room_points, hall)});

            }
            return out_grid;
        }

        private Grid AddHallAlone(in Grid grid, in IEnumerable<Vector2> room_points, in Hall hall)
        {
            Grid temp_grid = new Grid(grid.NRows, grid.NCols);
            (temp_grid, _, _) = AddHallInner(room_points, hall, temp_grid);

            return temp_grid;
        }

        private Grid AddHallToAll(Grid grid, IEnumerable<Vector2> room_points, Hall hall)
        {
            var (new_grid, inner_points, wall_points) = AddHallInner(room_points, hall, grid);
            grid = new_grid;
            hall.InsidePoints = new HashSet<Vector2>(inner_points);
            hall.WallPoints = new HashSet<Vector2>(wall_points);

            return grid;
        }

        private (Grid, IEnumerable<Vector2>, IEnumerable<Vector2>) AddHallInner(IEnumerable<Vector2> room_points, Hall hall, Grid grid)
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

        private (IEnumerable<Vector2>, IEnumerable<Vector2>) GenerateHall(Hall hall, IEnumerable<Vector2> room_inner)
        {
            HashSet<Vector2> inner_points = new HashSet<Vector2>(GenerateInsideHall(hall.Locus, hall.SourceLocus));
            inner_points.UnionWith(GenerateInsideHall(hall.Locus, hall.TargetLocus));
            var wall_points = GenerateHallWalls(inner_points);
            return (inner_points, wall_points);
        }

        private IEnumerable<Vector2> GenerateInsideHall(Vector2 source, Vector2 target)
        {
            return GoRogueWrapper.GetSimpleDirectHall(source, target);
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

        private bool InBounds(Vector2 location, Grid grid)
        {
            return location.X > 0 && location.Y > 0 && location.X <= grid.NCols && location.Y <= grid.NRows;
        }
    }
}