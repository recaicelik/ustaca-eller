using System;

namespace UstacaEller.Core.Geometry
{
    /// <summary>
    /// A 2D point. Deliberately not UnityEngine.Vector2: this assembly must compile
    /// and be testable without an engine, which is what lets the cutting geometry be
    /// verified in milliseconds instead of on a device.
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly float X;
        public readonly float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);

        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);

        public static Vec2 operator *(Vec2 a, float scalar) => new Vec2(a.X * scalar, a.Y * scalar);

        /// <summary>2D cross product magnitude. Positive when <paramref name="other"/> turns left of this.</summary>
        public float Cross(Vec2 other) => X * other.Y - Y * other.X;

        public float SqrMagnitude => X * X + Y * Y;

        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);

        public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());

        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}
