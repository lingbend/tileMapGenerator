namespace GoRogueWrapper
{
    using System.Numerics;
    using Vector2Extensions;
    using ConcRandom;
    using Grid;
    using SadRogue.Primitives.GridViews;
    using System;
    using System.Collections.Generic;

    public static class GoRogueWrapper
    {
        public static IEnumerable<Vector2> GetSimpleDirectHall(Vector2 start, Vector2 end)
        {
            Vector2 range = Vector2Ext.SpanRange(new Vector2[]{start, end});
            SimpleMapViewer view = new SimpleMapViewer(range);
            var chicken = new DirectLineTunnelCreator(AdjacencyRule.CARDINALS);
            chicken.CreateTunnel(view, start, end);
            return view.points;
        }

        public static IEnumerable<Vector2> GetSimpleHorizontalVerticalHall(Vector2 start, Vector2 end, object state, ConcRandom random)
        {
            Vector2 range = Vector2Ext.SpanRange(new Vector2[]{start, end});
            SimpleMapViewer view = new SimpleMapViewer(range);
            var chicken = new HorizontalVerticalTunnelCreator(random);
            lock (random)
            {
                random.global_state = start.ToString() + end.ToString() + state;
                chicken.CreateTunnel(view, start, end);
            }
        
            return view.points;
        }

        public static bool IsConnected(Vector2 start, Vector2 end, Grid grid, Vector2? locus = null)
        {
            BinaryMapViewer view = new BinaryMapViewer(grid, locus);
            var fast_astar = new FastAStar(view, SadRogue.Primitives.Distance.Manhattan);
            return fast_astar.ShortestPath((int) start.X, (int) start.Y, (int) end.X, (int) end.Y, false) != null;
        }

        internal class BinaryMapViewer : ISettableGridView<bool>
        {

            public int Height{get;}
            public int Width {get;}

            public int Count => Grid.Count;

            public bool this[SadRogue.Primitives.Point pos] { 
                get => InBounds(pos.X, pos.Y) ? Grid.GetCell((uint) pos.Y, (uint) pos.X) == 0 || (pos.X == Locus.X && pos.Y == Locus.Y) : false;
                set => Grid.SetCell((uint) pos.Y, (uint) pos.X, (value && InBounds(pos.X, pos.Y)) ? 0u : 1u); 
            }

            public HashSet<Vector2> points = new HashSet<Vector2>();
            private Grid Grid;
            private Vector2 Locus;

            public BinaryMapViewer(Grid grid, Vector2? locus = null)
            {
                Height = (int) grid.NRows;
                Width = (int) grid.NCols;
                Grid = new Grid(grid);
                if (locus != null)
                {
                    Locus = (Vector2) locus;
                }
                else
                {
                    Locus = Vector2.Zero;
                }
            
            }

            // public bool this[Coord pos] { 
            //     get => InBounds(pos.X, pos.Y) ? Grid.GetCell((uint) pos.Y, (uint) pos.X) == 0 || (pos.X == Locus.X && pos.Y == Locus.Y) : false;
            //     set => Grid.SetCell((uint) pos.Y, (uint) pos.X, (value && InBounds(pos.X, pos.Y)) ? 0u : 1u); 
            // }
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

        internal class SimpleMapViewer : ISettableGridView<bool>
        {

            public int Height{get;}
            public int Width {get;}
            public int Count => points.Count;
            public HashSet<Vector2> points = new HashSet<Vector2>();

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


            public bool this[SadRogue.Primitives.Point pos] { get => points.Contains(new Vector2(pos.X, pos.Y)); set => points.Add(new Vector2(pos.X, pos.Y)); }
            public bool this[int index1D] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool this[int x, int y] { get => points.Contains(new Vector2(x, y)); set => points.Add(new Vector2(x, y)); }

        
        }
    }

}
