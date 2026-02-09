namespace GoRogueWrapper;

using System.Numerics;
using GoRogue;
using GoRogue.MapGeneration;
using GoRogue.MapGeneration.Connectors;
using GoRogue.MapViews;
using GoRogue.Random;
using Vector2Extensions;
using ConcurrentRandom;
using GoRogue.Pathing;
using BinaryGrid;

public static class GoRogueWrapper
{
    public static IEnumerable<Vector2> GetSimpleDirectHall(Vector2 start, Vector2 end)
    {
        Vector2 range = Vector2Ext.SpanRange([start, end]);
        SimpleMapViewer view = new SimpleMapViewer(range);
        var chicken = new DirectLineTunnelCreator(AdjacencyRule.CARDINALS);
        chicken.CreateTunnel(view, (int) start.X, (int) start.Y, (int) end.X, (int) end.Y);
        return view.points;
    }

    public static IEnumerable<Vector2> GetSimpleHorizontalVerticalHall(Vector2 start, Vector2 end, object state, ConcurrentRandom random)
    {
        Vector2 range = Vector2Ext.SpanRange([start, end]);
        SimpleMapViewer view = new SimpleMapViewer(range);
        var chicken = new HorizontalVerticalTunnelCreator(random);
        lock (random)
        {
            random.global_state = start.ToString() + end.ToString() + state;
            chicken.CreateTunnel(view, (int) start.X, (int) start.Y, (int) end.X, (int) end.Y);
        }
        
        return view.points;
    }

    public static bool IsConnected(Vector2 start, Vector2 end, BinaryGrid grid, Vector2? locus = null)
    {
        BinaryMapViewer view = new BinaryMapViewer(grid, locus);
        var fast_astar = new FastAStar(view, Distance.MANHATTAN);
        return fast_astar.ShortestPath((int) start.X, (int) start.Y, (int) end.X, (int) end.Y, false) != null;
    }

    internal class BinaryMapViewer : ISettableMapView<bool>
    {

        public int Height{get;}
        public int Width {get;}
        public HashSet<Vector2> points = new();
        private BinaryGrid Grid;
        private Vector2 Locus;

        public BinaryMapViewer(BinaryGrid grid, Vector2? locus = null)
        {
            Height = (int) grid.RowSize;
            Width = (int) grid.ColSize;
            Grid = new BinaryGrid(grid);
            if (locus is not null)
            {
                Locus = (Vector2) locus;
            }
            else
            {
                Locus = Vector2.Zero;
            }
            
        }

        public bool this[Coord pos] { 
            get => InBounds(pos.X, pos.Y) ? Grid.GetCell((uint) pos.Y, (uint) pos.X) == 0 || (pos.X == Locus.X && pos.Y == Locus.Y) : false;
            set => Grid.SetCell((uint) pos.Y, (uint) pos.X, (value && InBounds(pos.X, pos.Y)) ? 0u : 1u); 
        }
        public bool this[int index1D] { 
            get => throw new NotImplementedException();
            set => throw new NotImplementedException(); 
        }
        public bool this[int x, int y] { 
            get => InBounds(x, y) ? Grid.GetCell((uint) y, (uint) x) == 0 || (x == Locus.X && y == Locus.Y): false;
            set => Grid.SetCell((uint) y, (uint) x, (value && InBounds(x, y)) ? 0u : 1u); 
        }  
        private bool InBounds(float x, float y)
        {
            if (x <= 0 || y <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    internal class SimpleMapViewer : ISettableMapView<bool>
    {

        public int Height{get;}
        public int Width {get;}
        public HashSet<Vector2> points = new();

        public SimpleMapViewer(int height, int width)
        {
            Height = height;
            Width = width;
        }

        public SimpleMapViewer(Vector2 range)
        {
            Height = (int) Math.Ceiling(range.Y) + 1;
            Width = (int) Math.Ceiling(range.X) + 1;
        }

        public bool this[Coord pos] { get => points.Contains(new Vector2(pos.X, pos.Y)); set => points.Add(new Vector2(pos.X, pos.Y)); }
        public bool this[int index1D] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool this[int x, int y] { get => points.Contains(new Vector2(x, y)); set => points.Add(new Vector2(x, y)); }

        
    }
}

