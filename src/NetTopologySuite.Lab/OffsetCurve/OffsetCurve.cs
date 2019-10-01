using System;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Noding;
using NetTopologySuite.Operation.Buffer;

namespace NetTopologySuite.OffsetCurve
{
    public class OffsetCurve
    {

        public static Geometry Compute(Geometry line, double distance)
        {
            return Compute(line, distance, new BufferParameters());
        }

        public static Geometry Compute(Geometry line, double distance, BufferParameters bufParams)
        {
            var curveRaw = ComputeRaw(line, distance, bufParams);
            var pts = curveRaw.Coordinates;
            var start = pts[0];
            var end = pts[pts.Length - 1];
            var noded = Node(curveRaw);

            //TODO: ensure start and end are nodes in noded geometry
            var path = ShortestPath.FindPath(noded, start, end);
            return path;
        }

        public static Geometry ComputePq(Geometry line, double distance)
        {
            return Compute(line, distance, new BufferParameters());
        }

        public static Geometry ComputePq(Geometry line, double distance, BufferParameters bufParams)
        {
            var curveRaw = ComputeRaw(line, distance, bufParams);
            var pts = curveRaw.Coordinates;
            var start = pts[0];
            var end = pts[pts.Length - 1];
            var noded = Node(curveRaw);
            Console.WriteLine(noded.AsText());

            //TODO: ensure start and end are nodes in noded geometry
            var path = ShortestPath.FindPathPq(noded, start, end);
            return path;
        }

        private static Geometry ComputeRaw(Geometry line, double distance, BufferParameters bufParams)
        {
            var ocb = new OffsetCurveBuilder(
                line.Factory.PrecisionModel, bufParams
            );
            var pts = ocb.GetOffsetCurve(line.Coordinates, distance);

            double distanceTol = distance * bufParams.SimplifyFactor;
            var ptsSimp = BufferInputLineSimplifier.Simplify(pts, distanceTol);
            var curve = line.Factory.CreateLineString(ptsSimp);
            return curve;
        }

        private static Geometry Node(Geometry geom)
        {
            var noder = new MCIndexNoder(new IntersectionAdder(new RobustLineIntersector()));
            noder.ComputeNodes(SegmentStringUtil.ExtractNodedSegmentStrings(geom));
            return SegmentStringUtil.ToGeometry(noder.GetNodedSubstrings(), geom.Factory);
        }
    }


}
