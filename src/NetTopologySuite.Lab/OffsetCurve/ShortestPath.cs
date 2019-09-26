using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using NetTopologySuite.EdgeGraph;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;

namespace NetTopologySuite.OffsetCurve
{
    /**
     * Dissolves the linear components 
     * from a collection of {@link Geometry}s
     * into a set of maximal-length {@link Linestring}s
     * in which every unique segment appears once only.
     * The output linestrings run between node vertices
     * of the input, which are vertices which have
     * either degree 1, or degree 3 or greater.
     * <p>
     * Use cases for dissolving linear components
     * include generalization 
     * (in particular, simplifying polygonal coverages), 
     * and visualization 
     * (in particular, avoiding symbology conflicts when
     * depicting shared polygon boundaries).
     * <p>
     * This class does <b>not</b> node the input lines.
     * If there are line segments crossing in the input, 
     * they will still cross in the output.
     * 
     * @author Martin Davis
     *
     */
    public class ShortestPath
    {
        /// <summary>
        /// Finds the shortest path through a Linear geometry from start to end.
        /// </summary>
        public static Geometry FindPath(Geometry g, Coordinate start, Coordinate end)
        {
            var d = new ShortestPath();
            d.Add(g);
            return d.GetResult(start, end);
        }

        private Geometry _result;
        private GeometryFactory _factory;

        //TODO: Is an EdgeGraph the most efficient structure for line sets which probably contain many long sequences of segments?
        private readonly EdgeGraph.EdgeGraph _graph;
        private Node _startNode;
        private Node _endNode;
        private readonly Dictionary<Coordinate, Node> _nodeMap;

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        public ShortestPath()
        {
            _nodeMap = new Dictionary<Coordinate, Node>();
            _graph = new EdgeGraph.EdgeGraph();
        }

        /// <summary>
        /// Adds a <see cref="Geometry"/> to be dissolved.
        /// Any number of geometries may be added by calling this method multiple times.
        /// Any type of Geometry may be added.  The constituent line-work will be
        /// extracted to be dissolved.
        /// </summary>
        /// <param name="geometry">geometry to be line-merged</param>
        public void Add(Geometry geometry)
        {
            geometry.Apply(new GeometryComponentFilter(FilterMethod));
        }

        private void FilterMethod(Geometry component)
        {
            if (component is LineString lsComponent)
            {
                Add(lsComponent);
            }
        }

        /// <summary>
        /// Adds a collection of Geometries to be processed. May be called multiple times.
        /// Any dimension of Geometry may be added; the constituent line-work will be
        /// extracted.
        /// </summary>
        /// <param name="geometries">the geometries to be line-merged</param>
        public void Add(IEnumerable<Geometry> geometries)
        {
            foreach (var geometry in geometries)
                Add(geometry);
        }


        //TODO: factor out the code to convert Linestrings to an EdgeGraph, since it's quite common
        private void Add(LineString lineString)
        {
            if (_factory == null)
            {
                _factory = lineString.Factory;
            }

            var seq = lineString.CoordinateSequence;
            //bool doneStart = false;
            for (int i = 1; i < seq.Count; i++)
            {
                var e = _graph.AddEdge(seq.GetCoordinate(i - 1), seq.GetCoordinate(i));
                // skip zero-length edges
                if (e == null) continue;
            }
        }

        /// <summary>
        /// Gets the dissolved result as a MultiLineString.
        /// </summary>
        /// <returns>
        /// the dissolved lines
        /// </returns>
        public Geometry GetResult(Coordinate start, Coordinate end)
        {
            if (_result == null)
                ComputeResult(start, end);
            return _result;
        }

        private void ComputeResult(Coordinate start, Coordinate end)
        {

            BuildNodes(start, end);
            FindShortestPath();
            var path = tracePath();
            _result = BuildLine(path);
        }

/**
 * Extract shortest path by backtracing shortest link pointers on nodes
 * @return
 */
        private List<Node> tracePath()
        {
            var path = new List<Node>();
            var node = _endNode;
            path.Add(_endNode);
            while (node != _startNode)
            {
                node = node.NearestOnPath;
                path.Add(node);
            }

            return path;
        }

        private Geometry BuildLine(IEnumerable<Node> path)
        {
            var coords = new CoordinateList();
            foreach (var n in path)
            {
                coords.Add(n.Coordinate, false);
            }
            return _factory.CreateLineString(coords.ToCoordinateArray());
        }

        private List<Node> FindShortestPathPq()
        {

            var path = new List<Node>();
            var uncommitted = new HashSet<Node>();
            var priorityQueue = new PriorityQueue<Node>();
            foreach (var value in _nodeMap.Values)
            {
                priorityQueue.Add(value);
                uncommitted.Add(value);
            }

            var currentNode = priorityQueue.Poll();
            while (true)
            {
                currentNode.SetMark(true);
                UpdateNeighbours(currentNode, uncommitted);

                currentNode = priorityQueue.Poll();
                uncommitted.Remove(currentNode);
                if (currentNode == null)
                {
                    break;
                }

                if (currentNode.Coordinate.Equals2D(_endNode.Coordinate))
                {
                    break;
                }

                // TODO: check if nearest in front has distance = infinity?  Means graph is disconnected
            }

            return path;
        }

        private List<Node> FindShortestPath()
        {

            var path = new List<Node>();
            var uncommitted = new HashSet<Node>();
            foreach (var value in _nodeMap.Values)
                uncommitted.Add(value);

            var currentNode = _startNode;
            while (true)
            {
                currentNode.SetMark(true);
                uncommitted.Remove(currentNode);
                UpdateNeighbours(currentNode, uncommitted);

                // TODO: linear search - speed up with priority queue?
                currentNode = Nearest(uncommitted);
                if (currentNode == null)
                {
                    break;
                }

                if (currentNode.Coordinate.Equals2D(_endNode.Coordinate))
                {
                    break;
                }

                // TODO: check if nearest in front has distance = infinity?  Means graph is disconnected
            }

            return path;
        }

        private void UpdateNeighbours(Node currentNode, HashSet<Node> uncommitted)
        {
            var start = currentNode.Edge;
            double currentDistance = currentNode.Distance;
            var next = start;
            do
            {
                var n = FindNode(next.Dest);
                if (uncommitted.Contains(n))
                {
                    double distToNodeFromCurrent = currentNode.Coordinate.Distance(n.Coordinate);
                    double dist = distToNodeFromCurrent + currentDistance;
                    if (dist < n.Distance)
                    {
                        n.Distance = dist;
                        n.NearestOnPath = currentNode;
                    }
                }

                next = next.ONext;
            } while (next != start);
        }

        private Node FindNode(Coordinate dest)
        {
            return _nodeMap[dest];
        }

        private Node NearestPq(PriorityQueue<Node> nodes)
        {
            return nodes.Poll();
        }

        private Node Nearest(ISet<Node> nodes)
        {
            Node nearest = null;
            foreach (var n in nodes)
            {
                if (nearest == null || n.Distance < nearest.Distance)
                {
                    nearest = n;
                }
            }
            return nearest;
        }

        private void BuildNodes(Coordinate start, Coordinate end)
        {
            var edges = _graph.GetVertexEdges();
            foreach (var e in edges)
            {
                var node = new Node(e);
                node.Distance = double.PositiveInfinity;
                _nodeMap.Add(node.Coordinate, node);
                if (node.Coordinate.Equals2D(start))
                {
                    _startNode = node;
                    node.Distance = 0;
                }
                else if (node.Coordinate.Equals2D(end))
                {
                    _endNode = node;
                }
            }

    }
}
