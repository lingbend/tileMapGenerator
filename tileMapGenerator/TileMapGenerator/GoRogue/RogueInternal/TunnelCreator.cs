/*
Adapted from the original by Benjamin Lingwall.
MIT License

Copyright (c) 2023 Christopher Ridley

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

namespace GoRogueWrapper
{
    using System.Numerics;
    using SadRogue.Primitives.GridViews;
    using Lines = SadRogue.Primitives.Lines;
    using Vector2Extensions;
    using System;
    using CRandom;
    public interface ITunnelCreator
    {
        void CreateTunnel(ISettableGridView<bool> map, Vector2 tunnelStart, Vector2 tunnelEnd);
    }

    public enum AdjacencyRule
    {
        DIAGONALS,
        CARDINALS
    }

    public class DirectLineTunnelCreator : ITunnelCreator
    {
        private readonly AdjacencyRule _adjacencyRule;
        private readonly bool _doubleWideVertical;

        public DirectLineTunnelCreator(AdjacencyRule adjacencyRule, bool doubleWideVertical = true)
        {
            if (adjacencyRule == AdjacencyRule.DIAGONALS)
                throw new ArgumentException("Cannot use diagonal adjacency to create tunnels", nameof(adjacencyRule));
            _adjacencyRule = adjacencyRule;
            _doubleWideVertical = doubleWideVertical;
        }

        public void CreateTunnel(ISettableGridView<bool> map, Vector2 start, Vector2 end)
        {
            var lineAlgorithm = _adjacencyRule == AdjacencyRule.CARDINALS
                ? Lines.Algorithm.Orthogonal
                : Lines.Algorithm.Bresenham;

            var previous = Vec2Ext.NONE;
            foreach (var pos in Lines.GetLine(start.ToPoint(), end.ToPoint(), lineAlgorithm))
            {
                map[pos] = true;
                if (_doubleWideVertical && previous != Vec2Ext.NONE && pos.Y != previous.Y && pos.X + 1 < map.Width - 1)
                {
                    var wideningPos = pos + (1, 0);
                    map[wideningPos] = true;
                }

                previous = pos.ToVector2();
            }
        }
    }

    public class HorizontalVerticalTunnelCreator : ITunnelCreator
    {
        private readonly CRandom _rng;

        public HorizontalVerticalTunnelCreator(CRandom? rng = null) => _rng = rng ?? new CRandom(1);

        public void CreateTunnel(ISettableGridView<bool> map, Vector2 tunnelStart, Vector2 tunnelEnd)
        {
            if (_rng.NextBool((map.Count * tunnelStart).GetHashCode()))
            {
                CreateHTunnel(map, (int) tunnelStart.X, (int) tunnelEnd.X, (int) tunnelStart.Y);
                CreateVTunnel(map, (int) tunnelStart.Y, (int) tunnelEnd.Y, (int) tunnelEnd.X);
            }
            else
            {
                CreateVTunnel(map, (int) tunnelStart.Y, (int) tunnelEnd.Y, (int) tunnelStart.X);
                CreateHTunnel(map, (int) tunnelStart.X, (int) tunnelEnd.X, (int) tunnelEnd.Y);
            }
        }

        private static void CreateHTunnel(ISettableGridView<bool> map, int xStart, int xEnd, int yPos)
        {
            for (var x = Math.Min(xStart, xEnd); x <= Math.Max(xStart, xEnd); ++x)
            {
                map[x, yPos] = true;
            }
        }

        private static void CreateVTunnel(ISettableGridView<bool> map, int yStart, int yEnd, int xPos)
        {
            for (var y = Math.Min(yStart, yEnd); y <= Math.Max(yStart, yEnd); ++y)
            {
                map[xPos, y] = true;
            }
        }
    }
}