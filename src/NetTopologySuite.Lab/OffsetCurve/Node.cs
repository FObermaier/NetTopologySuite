using System;
using NetTopologySuite.EdgeGraph;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.OffsetCurve
{
    public class Node : IComparable<Node>
    {

        //private readonly HalfEdge _edge;
        //private double distance;
        //private bool isMarked;
        //private Node nearestOnPath;

        public Node(HalfEdge e)
        {
            Edge = e;
        }

        public double Distance { get; set; }

        public Coordinate Coordinate
        {
            get => Edge.Orig;
        }

        public HalfEdge Edge { get; }

        int IComparable<Node>.CompareTo(Node other)
        {
            return Distance.CompareTo(other?.Distance ?? double.PositiveInfinity);
        }

        public override string ToString()
        {
            var p = Edge.Orig;
            return "[ " + p.X + ", " + p.Y + " d= " + Distance + " ]";
        }

        public void SetMark(bool value)
        {
            IsMarked = value;
        }

        public bool IsMarked { get; private set; }

        public Node NearestOnPath { get; set; }

    }
}
