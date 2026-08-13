using System;
using System.Collections.Generic;

namespace UstacaEller.Core.Geometry
{
    /// <summary>
    /// A simple (non self-intersecting) closed polygon. Winding order is not enforced;
    /// area is reported unsigned so callers never have to care.
    /// </summary>
    public sealed class Polygon
    {
        private readonly Vec2[] _vertices;

        public Polygon(IEnumerable<Vec2> vertices)
        {
            _vertices = new List<Vec2>(vertices).ToArray();
            if (_vertices.Length < 3)
            {
                throw new ArgumentException($"A polygon needs at least 3 vertices, got {_vertices.Length}.", nameof(vertices));
            }
        }

        public int Count => _vertices.Length;

        public IReadOnlyList<Vec2> Vertices => _vertices;

        /// <summary>Indexer that wraps, so <c>polygon[Count]</c> is <c>polygon[0]</c>.</summary>
        public Vec2 this[int index]
        {
            get
            {
                int wrapped = index % Count;
                if (wrapped < 0) wrapped += Count;
                return _vertices[wrapped];
            }
        }

        /// <summary>Positive when the vertices wind counter-clockwise.</summary>
        public float SignedArea()
        {
            double sum = 0;
            for (int i = 0; i < Count; i++)
            {
                Vec2 current = this[i];
                Vec2 next = this[i + 1];
                sum += (double)current.X * next.Y - (double)next.X * current.Y;
            }

            return (float)(sum * 0.5);
        }

        public float Area => Math.Abs(SignedArea());

        /// <summary>Ray casting. Points exactly on an edge are not guaranteed either way.</summary>
        public bool Contains(Vec2 point)
        {
            bool inside = false;
            for (int i = 0; i < Count; i++)
            {
                Vec2 a = this[i];
                Vec2 b = this[i + 1];
                bool straddles = (a.Y > point.Y) != (b.Y > point.Y);
                if (!straddles) continue;

                float crossingX = a.X + (point.Y - a.Y) / (b.Y - a.Y) * (b.X - a.X);
                if (point.X < crossingX) inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// Drops vertices that sit on top of their predecessor. Cutting produces these
        /// whenever the stroke crosses exactly through an existing vertex, and leaving
        /// them in makes downstream area and triangulation results unstable.
        /// </summary>
        public static IReadOnlyList<Vec2> RemoveDuplicates(IReadOnlyList<Vec2> points, float epsilon)
        {
            var cleaned = new List<Vec2>(points.Count);
            foreach (Vec2 point in points)
            {
                if (cleaned.Count > 0 && Vec2.Distance(cleaned[cleaned.Count - 1], point) <= epsilon) continue;
                cleaned.Add(point);
            }

            while (cleaned.Count > 1 && Vec2.Distance(cleaned[0], cleaned[cleaned.Count - 1]) <= epsilon)
            {
                cleaned.RemoveAt(cleaned.Count - 1);
            }

            return cleaned;
        }
    }
}
