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

    /// <summary>
    /// Implements a tunnel creation algorithm that sets as walkable a direct line between the two
    /// points. In the case that <see cref="SadRogue.Primitives.Distance.Manhattan" /> is being used, the line is calculated via the
    /// <see cref="SadRogue.Primitives.Lines.Algorithm.Orthogonal" /> algorithm.  Otherwise, the line is calculated using
    /// <see cref="SadRogue.Primitives.Lines.Algorithm.Bresenham" />.
    /// </summary>
    public class DirectLineTunnelCreator : ITunnelCreator
    {
        private readonly AdjacencyRule _adjacencyRule;
        private readonly bool _doubleWideVertical;

        /// <summary>
        /// Constructor. Takes the distance calculation to use, which determines whether <see cref="SadRogue.Primitives.Lines.Algorithm.Orthogonal" />
        /// or <see cref="SadRogue.Primitives.Lines.Algorithm.Bresenham" /> is used to create the tunnel.
        /// </summary>
        /// <param name="adjacencyRule">
        /// Method of adjacency to respect when creating tunnels. Cannot be diagonal.
        /// </param>
        /// <param name="doubleWideVertical">Whether or not to create vertical tunnels as 2-wide.</param>
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
                // Previous cell, and we're going vertical, go 2 wide so it looks nicer Make sure not
                // to break rectangles (less than last index)!
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