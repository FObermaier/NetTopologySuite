using System;
using NetTopologySuite.Geometries;
using NUnit.Framework;

namespace NetTopologySuite.Tests.NUnit.OffsetCurve
{
    public class OffsetCurveTest : GeometryTestCase
    {
        [TestCase(1d)]
        [TestCase(2.5d)]
        [TestCase(5d)]
        [TestCase(10d)]
        [TestCase(15d)]

        public void TestSimple(double buffer)
        {
            var geom = Read("LINESTRING(0 10, 125 10, 75 0, 200 0)");
            Geometry curve = null;
            Assert.That(() => curve = NetTopologySuite.OffsetCurve.OffsetCurve.Compute(geom, buffer), Throws.Nothing);
            Console.WriteLine(curve.AsText());
        }

        [TestCase(1d)]
        [TestCase(2.5d)]
        [TestCase(5d)]
        [TestCase(10d)]
        [TestCase(15d)]
        public void TestSimplePq(double buffer)
        {
            var geom = Read("LINESTRING(0 10, 125 10, 75 0, 200 0)");
            Geometry curve = null;
            Assert.That(() => curve = NetTopologySuite.OffsetCurve.OffsetCurve.ComputePq(geom, buffer), Throws.Nothing);
            Console.WriteLine(curve.AsText());
        }

    }
}
