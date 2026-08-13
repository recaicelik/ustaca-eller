using System;
using System.Collections.Generic;

namespace UstacaEller.Core.Geometry
{
    public enum CutOutcome
    {
        /// <summary>The stroke split the shape into two usable pieces.</summary>
        Cut,

        /// <summary>The stroke never crossed the outline twice, so nothing happened.</summary>
        NoCrossing,

        /// <summary>A piece would have been smaller than the configured minimum.</summary>
        PieceTooSmall,
    }

    public readonly struct CutResult
    {
        public readonly CutOutcome Outcome;
        public readonly Polygon PieceA;
        public readonly Polygon PieceB;

        private CutResult(CutOutcome outcome, Polygon pieceA, Polygon pieceB)
        {
            Outcome = outcome;
            PieceA = pieceA;
            PieceB = pieceB;
        }

        public bool Succeeded => Outcome == CutOutcome.Cut;

        public static CutResult Success(Polygon a, Polygon b) => new CutResult(CutOutcome.Cut, a, b);

        public static CutResult Failure(CutOutcome outcome) => new CutResult(outcome, null, null);
    }

    /// <summary>
    /// Splits a shape along a finger-drawn stroke.
    ///
    /// The mechanic has to survive a four-year-old dragging a finger across the screen
    /// in whatever direction they like, so the rules are deliberately forgiving:
    ///
    /// - The stroke is a polyline, not a straight line.
    /// - Only the first entry and the first following exit are used. Crossings
    ///   alternate inside/outside, so the sub-path between those two is guaranteed to
    ///   lie inside the shape. Later crossings are ignored rather than rejected —
    ///   a scribble that happens to cross again should still cut.
    /// - A cut that would shave off a sliver is refused outright and the shape stays
    ///   whole. Tiny pieces look broken and cannot be picked up by small fingers.
    /// </summary>
    public static class PolygonCutter
    {
        public const float DefaultEpsilon = 1e-4f;

        private readonly struct Crossing
        {
            public readonly int StrokeSegment;
            public readonly float StrokeT;
            public readonly int PolygonEdge;
            public readonly float EdgeT;
            public readonly Vec2 Point;

            public Crossing(int strokeSegment, float strokeT, int polygonEdge, float edgeT, Vec2 point)
            {
                StrokeSegment = strokeSegment;
                StrokeT = strokeT;
                PolygonEdge = polygonEdge;
                EdgeT = edgeT;
                Point = point;
            }
        }

        /// <param name="minPieceAreaRatio">
        /// Smallest allowed piece as a fraction of the original area. Mirrors
        /// <c>cut.minPieceArea</c> in the scene manifest.
        /// </param>
        public static CutResult Cut(
            Polygon polygon,
            IReadOnlyList<Vec2> stroke,
            float minPieceAreaRatio,
            float epsilon = DefaultEpsilon)
        {
            if (polygon == null) throw new ArgumentNullException(nameof(polygon));
            if (stroke == null) throw new ArgumentNullException(nameof(stroke));
            if (stroke.Count < 2) return CutResult.Failure(CutOutcome.NoCrossing);

            List<Crossing> crossings = FindCrossings(polygon, stroke, epsilon);
            if (crossings.Count < 2) return CutResult.Failure(CutOutcome.NoCrossing);

            Crossing entry = crossings[0];
            Crossing exit = crossings[1];

            IReadOnlyList<Vec2> insideStroke = StrokeVerticesBetween(stroke, entry, exit);

            Polygon pieceA = BuildPiece(polygon, entry, exit, insideStroke, reverseStroke: false, epsilon);
            Polygon pieceB = BuildPiece(polygon, exit, entry, insideStroke, reverseStroke: true, epsilon);
            if (pieceA == null || pieceB == null) return CutResult.Failure(CutOutcome.NoCrossing);

            float originalArea = polygon.Area;
            float smallest = Math.Min(pieceA.Area, pieceB.Area);
            if (originalArea <= 0f || smallest / originalArea < minPieceAreaRatio)
            {
                return CutResult.Failure(CutOutcome.PieceTooSmall);
            }

            return CutResult.Success(pieceA, pieceB);
        }

        private static List<Crossing> FindCrossings(Polygon polygon, IReadOnlyList<Vec2> stroke, float epsilon)
        {
            var crossings = new List<Crossing>();

            for (int s = 0; s < stroke.Count - 1; s++)
            {
                var segmentHits = new List<Crossing>();
                for (int e = 0; e < polygon.Count; e++)
                {
                    if (!SegmentIntersection(stroke[s], stroke[s + 1], polygon[e], polygon[e + 1],
                            out float strokeT, out float edgeT, out Vec2 point))
                    {
                        continue;
                    }

                    segmentHits.Add(new Crossing(s, strokeT, e, edgeT, point));
                }

                // Within one stroke segment the hits must still be ordered along the drag.
                segmentHits.Sort((left, right) => left.StrokeT.CompareTo(right.StrokeT));

                foreach (Crossing hit in segmentHits)
                {
                    // A stroke through a vertex hits both adjoining edges at the same
                    // point. Keeping both would make entry and exit the same location and
                    // produce a degenerate piece, so only the first is kept.
                    if (crossings.Count > 0 &&
                        Vec2.Distance(crossings[crossings.Count - 1].Point, hit.Point) <= epsilon)
                    {
                        continue;
                    }

                    crossings.Add(hit);
                }
            }

            return crossings;
        }

        private static IReadOnlyList<Vec2> StrokeVerticesBetween(IReadOnlyList<Vec2> stroke, Crossing entry, Crossing exit)
        {
            var inside = new List<Vec2>();
            for (int i = entry.StrokeSegment + 1; i <= exit.StrokeSegment; i++)
            {
                inside.Add(stroke[i]);
            }

            return inside;
        }

        /// <summary>
        /// One piece is the stroke from <paramref name="from"/> to <paramref name="to"/>
        /// followed by the outline walked forward from <paramref name="to"/> back around
        /// to <paramref name="from"/>. The other piece is the same construction with the
        /// stroke reversed, which is why both share this method.
        /// </summary>
        private static Polygon BuildPiece(
            Polygon polygon,
            Crossing from,
            Crossing to,
            IReadOnlyList<Vec2> insideStroke,
            bool reverseStroke,
            float epsilon)
        {
            var points = new List<Vec2> { from.Point };

            if (reverseStroke)
            {
                for (int i = insideStroke.Count - 1; i >= 0; i--) points.Add(insideStroke[i]);
            }
            else
            {
                points.AddRange(insideStroke);
            }

            points.Add(to.Point);
            points.AddRange(WalkBoundary(polygon, to, from));

            IReadOnlyList<Vec2> cleaned = Polygon.RemoveDuplicates(points, epsilon);
            return cleaned.Count < 3 ? null : new Polygon(cleaned);
        }

        /// <summary>
        /// Vertices met while walking the outline forward from one crossing to another.
        /// Returns empty when both crossings sit on the same edge in walking order —
        /// that is the "small nick" case, which the area check then rejects.
        /// </summary>
        private static List<Vec2> WalkBoundary(Polygon polygon, Crossing from, Crossing to)
        {
            var vertices = new List<Vec2>();
            int edge = from.PolygonEdge;
            bool moved = false;

            for (int guard = 0; guard <= polygon.Count + 1; guard++)
            {
                if (edge == to.PolygonEdge && (moved || to.EdgeT >= from.EdgeT)) return vertices;
                vertices.Add(polygon[edge + 1]);
                edge = (edge + 1) % polygon.Count;
                moved = true;
            }

            return vertices;
        }

        private static bool SegmentIntersection(
            Vec2 p1, Vec2 p2, Vec2 q1, Vec2 q2,
            out float t, out float u, out Vec2 point)
        {
            Vec2 r = p2 - p1;
            Vec2 s = q2 - q1;
            float denominator = r.Cross(s);

            if (Math.Abs(denominator) < 1e-9f)
            {
                t = 0f;
                u = 0f;
                point = default;
                return false;
            }

            Vec2 delta = q1 - p1;
            t = delta.Cross(s) / denominator;
            u = delta.Cross(r) / denominator;
            point = p1 + r * t;

            return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
        }
    }
}
