using System.Collections.Generic;
using UstacaEller.Core.Geometry;
using Xunit;

namespace UstacaEller.Core.Tests
{
    public class CuttableShapeTests
    {
        private const float Tolerance = 0.01f;

        /// <summary>Dough on the counter: an 8x8 square, area 64.</summary>
        private static Polygon Dough() => new Polygon(new[]
        {
            new Vec2(0, 0),
            new Vec2(8, 0),
            new Vec2(8, 8),
            new Vec2(0, 8),
        });

        private static List<Vec2> VerticalStrokeAt(float x) =>
            new List<Vec2> { new Vec2(x, -1), new Vec2(x, 9) };

        private static List<Vec2> HorizontalStrokeAt(float y) =>
            new List<Vec2> { new Vec2(-1, y), new Vec2(9, y) };

        [Fact]
        public void StartsAsOneWholePiece()
        {
            var shape = new CuttableShape(Dough(), minPieceAreaRatio: 0.08f, maxPieces: 6);

            Assert.Single(shape.Pieces);
            Assert.Equal(64f, shape.OriginalArea, Tolerance);
        }

        [Fact]
        public void ACutReplacesOnePieceWithTwo()
        {
            var shape = new CuttableShape(Dough(), minPieceAreaRatio: 0.08f, maxPieces: 6);

            Assert.Equal(ShapeCutOutcome.Cut, shape.TryCut(VerticalStrokeAt(4)));

            Assert.Equal(2, shape.Pieces.Count);
            Assert.Equal(64f, TotalArea(shape), Tolerance);
        }

        [Fact]
        public void SuccessiveCutsKeepSplittingWhicheverPieceIsCrossed()
        {
            var shape = new CuttableShape(Dough(), minPieceAreaRatio: 0.05f, maxPieces: 6);

            shape.TryCut(VerticalStrokeAt(4));
            shape.TryCut(HorizontalStrokeAt(4));

            Assert.Equal(3, shape.Pieces.Count);
            Assert.Equal(64f, TotalArea(shape), Tolerance);
        }

        [Fact]
        public void MinimumSizeIsMeasuredAgainstTheOriginalNotTheCurrentPiece()
        {
            // Cut in half, then try to shave a piece that is 10% of the remaining half
            // but only 5% of the original. With the ratio at 8% this must be refused —
            // otherwise every cut lowers the bar and the shape ends up as confetti.
            var shape = new CuttableShape(Dough(), minPieceAreaRatio: 0.08f, maxPieces: 8);
            shape.TryCut(VerticalStrokeAt(4));

            ShapeCutOutcome outcome = shape.TryCut(VerticalStrokeAt(0.4f));

            Assert.Equal(ShapeCutOutcome.PieceTooSmall, outcome);
            Assert.Equal(2, shape.Pieces.Count);
        }

        [Fact]
        public void ReachingThePieceLimitQuietlyStopsCutting()
        {
            var shape = new CuttableShape(Dough(), minPieceAreaRatio: 0.05f, maxPieces: 3);
            shape.TryCut(VerticalStrokeAt(3));
            shape.TryCut(VerticalStrokeAt(6));

            Assert.Equal(3, shape.Pieces.Count);
            Assert.False(shape.CanCutFurther);

            // Nothing breaks, nothing is lost, the child is not told off.
            Assert.Equal(ShapeCutOutcome.PieceLimitReached, shape.TryCut(HorizontalStrokeAt(4)));
            Assert.Equal(3, shape.Pieces.Count);
            Assert.Equal(64f, TotalArea(shape), Tolerance);
        }

        [Fact]
        public void AStrokeThatMissesEverythingChangesNothing()
        {
            var shape = new CuttableShape(Dough(), minPieceAreaRatio: 0.08f, maxPieces: 6);
            shape.TryCut(VerticalStrokeAt(4));

            ShapeCutOutcome outcome = shape.TryCut(new List<Vec2> { new Vec2(50, 50), new Vec2(60, 60) });

            Assert.Equal(ShapeCutOutcome.NoCrossing, outcome);
            Assert.Equal(2, shape.Pieces.Count);
        }

        [Fact]
        public void AStrokeAcrossASecondPieceFindsThatPiece()
        {
            // The first piece is untouched by this stroke; the search must not give up
            // on the first miss.
            var shape = new CuttableShape(Dough(), minPieceAreaRatio: 0.05f, maxPieces: 6);
            shape.TryCut(VerticalStrokeAt(4));

            ShapeCutOutcome outcome = shape.TryCut(new List<Vec2> { new Vec2(5, -1), new Vec2(5, 9) });

            Assert.Equal(ShapeCutOutcome.Cut, outcome);
            Assert.Equal(3, shape.Pieces.Count);
        }

        [Fact]
        public void AShapeMustAllowAtLeastTwoPieces()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new CuttableShape(Dough(), minPieceAreaRatio: 0.08f, maxPieces: 1));
        }

        private static float TotalArea(CuttableShape shape)
        {
            float total = 0f;
            foreach (Polygon piece in shape.Pieces) total += piece.Area;
            return total;
        }
    }
}
