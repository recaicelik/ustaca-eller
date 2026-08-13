using System;
using System.Collections.Generic;

namespace UstacaEller.Core.Geometry
{
    public enum ShapeCutOutcome
    {
        /// <summary>A piece was split in two.</summary>
        Cut,

        /// <summary>The stroke did not cross any piece twice.</summary>
        NoCrossing,

        /// <summary>A resulting piece would have been below the minimum size.</summary>
        PieceTooSmall,

        /// <summary>The shape already holds as many pieces as the scene allows.</summary>
        PieceLimitReached,
    }

    /// <summary>
    /// One cuttable object in a scene, across a whole play session.
    ///
    /// A single cut is geometry; a session is a sequence of them, and the limits in
    /// the manifest (<c>cut.maxPieces</c>, <c>cut.minPieceArea</c>) only mean anything
    /// at that level. Two rules follow from watching how the age group actually plays:
    ///
    /// - Minimum piece size is measured against the <em>original</em> area, not the
    ///   piece being cut. Otherwise each cut lowers the bar and the tenth one is
    ///   allowed to produce confetti.
    /// - Hitting the piece limit is not an error state. The stroke simply does
    ///   nothing, the shape stays as it is, and nothing tells the child off.
    /// </summary>
    public sealed class CuttableShape
    {
        private readonly List<Polygon> _pieces = new List<Polygon>();

        public CuttableShape(Polygon shape, float minPieceAreaRatio, int maxPieces)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (maxPieces < 2) throw new ArgumentOutOfRangeException(nameof(maxPieces), "A cuttable shape must allow at least 2 pieces.");

            _pieces.Add(shape);
            OriginalArea = shape.Area;
            MinPieceAreaRatio = minPieceAreaRatio;
            MaxPieces = maxPieces;
        }

        public IReadOnlyList<Polygon> Pieces => _pieces;

        public float OriginalArea { get; }

        public float MinPieceAreaRatio { get; }

        public int MaxPieces { get; }

        public bool CanCutFurther => _pieces.Count < MaxPieces;

        /// <summary>
        /// Applies a stroke. The first piece the stroke actually splits is replaced by
        /// its two halves; the rest are untouched.
        /// </summary>
        public ShapeCutOutcome TryCut(IReadOnlyList<Vec2> stroke, float epsilon = PolygonCutter.DefaultEpsilon)
        {
            if (stroke == null) throw new ArgumentNullException(nameof(stroke));
            if (!CanCutFurther) return ShapeCutOutcome.PieceLimitReached;

            // A near miss on one piece must not stop us finding the piece the child
            // meant, so every piece is tried before reporting failure. The best failure
            // seen is reported, because "too small" is more informative than "missed".
            ShapeCutOutcome bestFailure = ShapeCutOutcome.NoCrossing;

            for (int index = 0; index < _pieces.Count; index++)
            {
                CutResult result = PolygonCutter.CutAgainst(
                    _pieces[index], stroke, MinPieceAreaRatio, OriginalArea, epsilon);

                if (result.Succeeded)
                {
                    _pieces[index] = result.PieceA;
                    _pieces.Insert(index + 1, result.PieceB);
                    return ShapeCutOutcome.Cut;
                }

                if (result.Outcome == CutOutcome.PieceTooSmall) bestFailure = ShapeCutOutcome.PieceTooSmall;
            }

            return bestFailure;
        }
    }
}
