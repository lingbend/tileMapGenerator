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
    using Lines = SadRogue.Primitives.Lines;
    using SadRogue.Primitives.GridViews;
    using Vector2Extensions;
    using System;

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

            var previous = Vector2Ext.NONE;
            foreach (var pos in Lines.GetLine(start.ToPoint(), end.ToPoint(), lineAlgorithm))
            {
                map[pos] = true;
                if (_doubleWideVertical && previous != Vector2Ext.NONE && pos.Y != previous.Y && pos.X + 1 < map.Width - 1)
                {
                    var wideningPos = pos + (1, 0);
                    map[wideningPos] = true;
                }

                previous = pos.ToVector2();
            }
        }
    }
}