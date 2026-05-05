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
    /// <summary>
    /// Interface for implementing an algorithm for creating a tunnel between two positions on a
    /// walkability map.
    /// </summary>
    public interface ITunnelCreator
    {
        /// <summary>
        /// Implements the algorithm, creating the tunnel between the two points (ensuring there is a
        /// path of positions set to true between those two points).
        /// </summary>
        /// <param name="map">_grid to create the tunnel on.</param>
        /// <param name="tunnelStart">Start position to connect.</param>
        /// <param name="tunnelEnd">End position to connect.</param>
        /// <returns>An area containing all points that are part of the tunnel.</returns>
        void CreateTunnel(ISettableGridView<bool> map, Vector2 tunnelStart, Vector2 tunnelEnd);
    }
}