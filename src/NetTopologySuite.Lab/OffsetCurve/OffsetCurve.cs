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
            var curveRaw = ComputeRaw(line, distance);
            var pts = curveRaw.Coordinates;
            var start = pts[0];
            var end = pts[pts.Length - 1];
            var noded = Node(curveRaw);
            //TODO: ensure start and end are nodes in noded geometry
            var path = ShortestPath.FindPath(noded, start, end);
            return path;
        }

        private static Geometry ComputeRaw(Geometry line, double distance)
        {
            var bufParams = new BufferParameters();
            var ocb = new OffsetCurveBuilder(
                line.Factory.PrecisionModel, bufParams
            );
            var pts = ocb.GetOffsetCurve(line.Coordinates, distance);

            var ptsSimp = BufferInputLineSimplifier.Simplify(pts, distance);

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
