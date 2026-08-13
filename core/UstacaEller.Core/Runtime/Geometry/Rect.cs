using System;

namespace UstacaEller.Core.Geometry
{
    /// <summary>Axis-aligned rectangle. Matches the <c>shape</c> block in a scene manifest.</summary>
    public readonly struct Rect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public Rect(float x, float y, float width, float height)
        {
            if (width <= 0f) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float MaxX => X + Width;

        public float MaxY => Y + Height;

        public Vec2 Center => new Vec2(X + Width * 0.5f, Y + Height * 0.5f);

        public bool Contains(Vec2 point) =>
            point.X >= X && point.X <= MaxX && point.Y >= Y && point.Y <= MaxY;

        /// <summary>Nearest point inside the rectangle. Equals <paramref name="point"/> when already inside.</summary>
        public Vec2 ClosestPoint(Vec2 point) => new Vec2(
            Math.Min(Math.Max(point.X, X), MaxX),
            Math.Min(Math.Max(point.Y, Y), MaxY));

        public float DistanceTo(Vec2 point) => Vec2.Distance(point, ClosestPoint(point));

        public override string ToString() => $"[{X:0.#}, {Y:0.#}, {Width:0.#}x{Height:0.#}]";
    }
}
