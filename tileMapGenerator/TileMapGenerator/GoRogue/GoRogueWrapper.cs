namespace GoRogueWrapper;

using System.Numerics;
using GoRogue;
using GoRogue.MapGeneration;
using GoRogue.MapGeneration.Connectors;
using GoRogue.MapViews;
using GoRogue.Random;
using Vector2Extensions;
using ConcurrentRandom;

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

