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
    using ConcurrentRandom;
    using SadRogue.Primitives.GridViews;
    using System;

    /// <summary>
    /// Implements a tunnel creation algorithm that creates a tunnel that performs all needed
    /// vertical movement before horizontal movement, or vice versa (depending on rng).
    /// </summary>
    public class HorizontalVerticalTunnelCreator : ITunnelCreator
    {
        private readonly ConcurrentRandom _rng;

        /// <summary>
        /// Creates a new tunnel creator.
        /// </summary>
        /// <param name="rng">RNG to use for movement selection.</param>
        public HorizontalVerticalTunnelCreator(ConcurrentRandom? rng = null) => _rng = rng ?? new ConcurrentRandom(1);

        public void CreateTunnel(ISettableGridView<bool> map, Vector2 tunnelStart, Vector2 tunnelEnd)
        {
            if (_rng.NextBool((map.Count * tunnelStart).GetHashCode().ToString()))
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