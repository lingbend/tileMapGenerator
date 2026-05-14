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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Priority_Queue;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;

namespace GoRogueWrapper
{
    public class AStar
    {
        private int _cachedHeight;

        private double _cachedMinWeight;
        private int _cachedWidth;

        private BitArray _closed;

        private Func<Point, Point, double> _heuristic;

        private AStarNode?[] _nodes;

        private FastPriorityQueue<AStarNode> _openNodes;

        public double MinimumWeight;

        public AStar(IGridView<bool> walkabilityView, Distance distanceMeasurement)
            : this(walkabilityView, distanceMeasurement, null, null, 1.0)
        { }

        public AStar(IGridView<bool> walkabilityView, Distance distanceMeasurement, Func<Point, Point, double> heuristic)
            : this(walkabilityView, distanceMeasurement, heuristic, null, -1.0)
        { }

        public AStar(IGridView<bool> walkabilityView, Distance distanceMeasurement, IGridView<double> weights,
                     double minimumWeight)
            : this(walkabilityView, distanceMeasurement, null, weights, minimumWeight)
        { }

        public AStar(IGridView<bool> walkabilityView, Distance distanceMeasurement, Func<Point, Point, double> heuristic,
                     IGridView<double> weights)
            : this(walkabilityView, distanceMeasurement, heuristic, weights, -1.0)
        { }

#pragma warning disable CS8618
        private AStar(IGridView<bool> walkabilityView, Distance distanceMeasurement,
                      Func<Point, Point, double>? heuristic = null, IGridView<double>? weights = null,
                      double minimumWeight = 1.0)
#pragma warning restore CS8618
        {
            Weights = weights;

            WalkabilityView = walkabilityView;
            DistanceMeasurement = distanceMeasurement;
            MinimumWeight = minimumWeight;
            _cachedMinWeight = minimumWeight;
            MaxEuclideanMultiplier = MinimumWeight / Point.EuclideanDistanceMagnitude(new Point(0, 0),
                new Point(WalkabilityView.Width, WalkabilityView.Height));

            Heuristic = heuristic!; 

            var maxSize = walkabilityView.Width * walkabilityView.Height;
            _nodes = new AStarNode?[maxSize];
            _closed = new BitArray(maxSize);
            _cachedWidth = walkabilityView.Width;
            _cachedHeight = walkabilityView.Height;

            _openNodes = new FastPriorityQueue<AStarNode>(maxSize);
        }

        public Distance DistanceMeasurement { get; set; }

        public IGridView<bool> WalkabilityView { get; private set; }

        [AllowNull]
        public Func<Point, Point, double> Heuristic
        {
            get => _heuristic;

            set => _heuristic = value ?? ((c1, c2)
                => DistanceMeasurement.Calculate(c1, c2) +
                   Point.EuclideanDistanceMagnitude(c1, c2) * MaxEuclideanMultiplier);
        }

        public IGridView<double>? Weights { get; }

        public double MaxEuclideanMultiplier { get; private set; }

        public Path? ShortestPath(Point start, Point end, bool assumeEndpointsWalkable = true)
        {
            SadRogue.Primitives.AdjacencyRule adjacencyRule = (SadRogue.Primitives.AdjacencyRule) DistanceMeasurement;

            if (!assumeEndpointsWalkable && (!WalkabilityView[start] || !WalkabilityView[end]))
                return null; 

            if (start == end)
            {
                var retVal = new List<Point> { start };
                return new Path(retVal, 0);
            }

            if (MinimumWeight != _cachedMinWeight)
            {
                _cachedMinWeight = MinimumWeight;
                MaxEuclideanMultiplier = MinimumWeight / Point.EuclideanDistanceMagnitude(new Point(0, 0),
                    new Point(WalkabilityView.Width, WalkabilityView.Height));
            }

            if (_cachedWidth != WalkabilityView.Width || _cachedHeight != WalkabilityView.Height)
            {
                var length = WalkabilityView.Width * WalkabilityView.Height;
                _nodes = new AStarNode[length];
                _closed = new BitArray(length);
                _openNodes = new FastPriorityQueue<AStarNode>(length);

                _cachedWidth = WalkabilityView.Width;
                _cachedHeight = WalkabilityView.Height;

                MaxEuclideanMultiplier = MinimumWeight / Point.EuclideanDistanceMagnitude(new Point(0, 0),
                    new Point(WalkabilityView.Width, WalkabilityView.Height));
            }
            else
                _closed.SetAll(false);

            var result = new List<Point>();
            var index = start.ToIndex(WalkabilityView.Width);

            _nodes[index] ??= new AStarNode(start);

            _nodes[index]!.G = 0;
            _nodes[index]!.F = (float)Heuristic(start, end); 
            _openNodes.Enqueue(_nodes[index]!, _nodes[index]!.F);

            while (_openNodes.Count != 0)
            {
                var current = _openNodes.Dequeue();
                var currentIndex = current.Position.ToIndex(WalkabilityView.Width);
                _closed[currentIndex] = true;

                if (current.Position == end) 
                {
                    _openNodes.Clear();
                    double cost = current.G;
                    do
                    {
                        result.Add(current.Position);
                        current = current.Parent!;
                    } while (current.Position != start);

                    result.Add(start);
                    return new Path(result, cost);
                }

                for (int i = 0; i < adjacencyRule.DirectionsOfNeighborsCache.Length; i++)
                {
                    var neighborPos = current.Position + adjacencyRule.DirectionsOfNeighborsCache[i];

                    if (neighborPos.X < 0 || neighborPos.Y < 0 || neighborPos.X >= WalkabilityView.Width ||
                        neighborPos.Y >= WalkabilityView.Height)
                        continue;

                    if (!CheckWalkability(neighborPos, start, end, assumeEndpointsWalkable)) 
                        continue;

                    var neighborIndex = neighborPos.ToIndex(WalkabilityView.Width);
                    var neighbor = _nodes[neighborIndex];

                    var isNeighborOpen = IsOpen(neighbor, _openNodes);

                    if (neighbor == null) 
                        _nodes[neighborIndex] = neighbor = new AStarNode(neighborPos);
                    else if (_closed[neighborIndex]) 
                        continue;

                    var newDistance =
                        current.G + (float)DistanceMeasurement.Calculate(current.Position, neighbor.Position) *
                        (float)(Weights?[neighbor.Position] ?? 1.0);
                    if (isNeighborOpen && newDistance >= neighbor.G) 
                        continue;

                    neighbor.Parent = current;
                    neighbor.G = newDistance; 
                    neighbor.F = newDistance + (float)Heuristic(neighbor.Position, end);

                    if (_openNodes.Contains(neighbor))
                        _openNodes.UpdatePriority(neighbor, neighbor.F);
                    else 
                        _openNodes.Enqueue(neighbor, neighbor.F);
                }
            }

            _openNodes.Clear();
            return null; 
        }

        public Path? ShortestPath(int startX, int startY, int endX, int endY, bool assumeEndpointsWalkable = true)
            => ShortestPath(new Point(startX, startY), new Point(endX, endY), assumeEndpointsWalkable);

        private static bool IsOpen(AStarNode? node, FastPriorityQueue<AStarNode> openSet)
            => node != null && openSet.Contains(node);

        private bool CheckWalkability(Point pos, Point start, Point end, bool assumeEndpointsWalkable)
        {
            if (!assumeEndpointsWalkable)
                return WalkabilityView[pos];

            return WalkabilityView[pos] || pos == start || pos == end;
        }
    }

    public class Path
    {
        private double _cost;
        private readonly IReadOnlyList<Point> _steps;
        private bool _inOriginalOrder;

        public Path(Path pathToCopy, bool reverse = false)
        {
            _steps = pathToCopy._steps;
            _cost = pathToCopy._cost;
            _inOriginalOrder = reverse ? !pathToCopy._inOriginalOrder : pathToCopy._inOriginalOrder;
        }

        internal Path(IReadOnlyList<Point> steps, double cost)
        {
            _steps = steps;
            _inOriginalOrder = true;
            _cost = cost;
        }

        public Point End => _inOriginalOrder ? _steps[0] : _steps[^1];

        public int Length => _steps.Count - 1;

        public int LengthWithStart => _steps.Count;

        public Point Start => _inOriginalOrder ? _steps[^1] : _steps[0];

        public double Cost => _cost;

        public IEnumerable<Point> Steps
        {
            get
            {
                if (_inOriginalOrder)
                    for (var i = _steps.Count - 2; i >= 0; i--)
                        yield return _steps[i];
                else
                    for (var i = 1; i < _steps.Count; i++)
                        yield return _steps[i];
            }
        }

        public IEnumerable<Point> StepsWithStart
        {
            get
            {
                if (_inOriginalOrder)
                    for (var i = _steps.Count - 1; i >= 0; i--)
                        yield return _steps[i];
                else
                    foreach (var step in _steps)
                        yield return step;
            }
        }

        public Point GetStep(int stepNum)
        {
            if (_inOriginalOrder)
                return _steps[_steps.Count - 2 - stepNum];

            return _steps[stepNum + 1];
        }

        public Point GetStepWithStart(int stepNum) =>
            _inOriginalOrder ? _steps[_steps.Count - 1 - stepNum] : _steps[stepNum];

        public void Reverse() => _inOriginalOrder = !_inOriginalOrder;

        public override string ToString() => StepsWithStart.ExtendToString();
    }

    internal class AStarNode : FastPriorityQueueNode
    {
        public readonly Point Position;

        public float F;
        public float G;

        public AStarNode? Parent;

        public AStarNode(Point position, AStarNode? parent = null)
        {
            Parent = parent;
            Position = position;
            F = G = float.MaxValue;
        }
    }
}