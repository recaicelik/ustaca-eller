using System.Collections.Generic;
using UstacaEller.Core.Geometry;
using Xunit;

namespace UstacaEller.Core.Tests
{
    public class PolygonCutterTests
    {
        private const float Tolerance = 0.01f;

        /// <summary>A 4x4 square with corners at the origin. Area 16.</summary>
        private static Polygon Square() => new Polygon(new[]
        {
            new Vec2(0, 0),
            new Vec2(4, 0),
            new Vec2(4, 4),
            new Vec2(0, 4),
        });

        /// <summary>An L shape, so the concave case is covered. Area 12.</summary>
        private static Polygon LShape() => new Polygon(new[]
        {
            new Vec2(0, 0),
            new Vec2(4, 0),
            new Vec2(4, 2),
            new Vec2(2, 2),
            new Vec2(2, 4),
            new Vec2(0, 4),
        });

        private static List<Vec2> Stroke(params (float X, float Y)[] points)
        {
            var stroke = new List<Vec2>(points.Length);
            foreach ((float x, float y) in points) stroke.Add(new Vec2(x, y));
            return stroke;
        }

        [Fact]
        public void StraightCutSplitsSquareInHalf()
        {
            CutResult result = PolygonCutter.Cut(Square(), Stroke((2, -1), (2, 5)), minPieceAreaRatio: 0.05f);

            Assert.Equal(CutOutcome.Cut, result.Outcome);
            Assert.Equal(8f, result.PieceA.Area, Tolerance);
            Assert.Equal(8f, result.PieceB.Area, Tolerance);
        }

        [Fact]
        public void CutPreservesTotalArea()
        {
            Polygon square = Square();

            CutResult result = PolygonCutter.Cut(square, Stroke((-1, 1), (5, 3)), minPieceAreaRatio: 0.05f);

            Assert.True(result.Succeeded);
            Assert.Equal(square.Area, result.PieceA.Area + result.PieceB.Area, Tolerance);
        }

        [Fact]
        public void StrokeThatMissesTheShapeDoesNothing()
        {
            CutResult result = PolygonCutter.Cut(Square(), Stroke((10, 10), (12, 12)), minPieceAreaRatio: 0.05f);

            Assert.Equal(CutOutcome.NoCrossing, result.Outcome);
            Assert.Null(result.PieceA);
        }

        [Fact]
        public void StrokeThatStaysInsideDoesNotCut()
        {
            // A scribble that never reaches the outline. Common: the child draws on the
            // dough rather than across it, and the shape must survive untouched.
            CutResult result = PolygonCutter.Cut(Square(), Stroke((1, 1), (3, 3)), minPieceAreaRatio: 0.05f);

            Assert.Equal(CutOutcome.NoCrossing, result.Outcome);
        }

        [Fact]
        public void SliverCutIsRefusedAndShapeStaysWhole()
        {
            // Cutting 1.25% off the edge. The manifest asks for at least 8%.
            CutResult result = PolygonCutter.Cut(Square(), Stroke((0.05f, -1), (0.05f, 5)), minPieceAreaRatio: 0.08f);

            Assert.Equal(CutOutcome.PieceTooSmall, result.Outcome);
            Assert.Null(result.PieceA);
        }

        [Fact]
        public void SliverIsAllowedWhenTheSceneAsksForALowerMinimum()
        {
            CutResult result = PolygonCutter.Cut(Square(), Stroke((0.05f, -1), (0.05f, 5)), minPieceAreaRatio: 0.01f);

            Assert.Equal(CutOutcome.Cut, result.Outcome);
        }

        [Fact]
        public void ZigZagStrokeStillCuts()
        {
            // Fingers do not draw straight lines.
            CutResult result = PolygonCutter.Cut(
                Square(),
                Stroke((2, -1), (1.6f, 1), (2.4f, 2), (1.8f, 3), (2, 5)),
                minPieceAreaRatio: 0.05f);

            Assert.Equal(CutOutcome.Cut, result.Outcome);
            Assert.Equal(16f, result.PieceA.Area + result.PieceB.Area, Tolerance);
        }

        [Fact]
        public void ConcaveShapeCutsCorrectly()
        {
            Polygon shape = LShape();

            CutResult result = PolygonCutter.Cut(shape, Stroke((1, -1), (1, 5)), minPieceAreaRatio: 0.05f);

            Assert.Equal(CutOutcome.Cut, result.Outcome);
            Assert.Equal(shape.Area, result.PieceA.Area + result.PieceB.Area, Tolerance);
            Assert.Equal(4f, System.Math.Min(result.PieceA.Area, result.PieceB.Area), Tolerance);
        }

        [Fact]
        public void CuttingThroughAVertexProducesValidPieces()
        {
            // The stroke passes exactly through (4,0) and (0,4). Without duplicate-point
            // cleanup this produces degenerate rings.
            CutResult result = PolygonCutter.Cut(Square(), Stroke((5, -1), (-1, 5)), minPieceAreaRatio: 0.05f);

            Assert.Equal(CutOutcome.Cut, result.Outcome);
            Assert.True(result.PieceA.Count >= 3);
            Assert.True(result.PieceB.Count >= 3);
            Assert.Equal(16f, result.PieceA.Area + result.PieceB.Area, Tolerance);
        }

        [Fact]
        public void PieceCanBeCutAgain()
        {
            // Sequential cuts are how a child reaches several pieces; each cut operates
            // on one existing piece rather than the original shape.
            CutResult first = PolygonCutter.Cut(Square(), Stroke((2, -1), (2, 5)), minPieceAreaRatio: 0.05f);
            Assert.True(first.Succeeded);

            CutResult second = PolygonCutter.Cut(first.PieceA, Stroke((-1, 2), (5, 2)), minPieceAreaRatio: 0.05f);

            Assert.True(second.Succeeded);
            Assert.Equal(first.PieceA.Area, second.PieceA.Area + second.PieceB.Area, Tolerance);
        }

        [Fact]
        public void SingleTapIsNotACut()
        {
            CutResult result = PolygonCutter.Cut(Square(), Stroke((2, 2)), minPieceAreaRatio: 0.05f);

            Assert.Equal(CutOutcome.NoCrossing, result.Outcome);
        }
    }
}
