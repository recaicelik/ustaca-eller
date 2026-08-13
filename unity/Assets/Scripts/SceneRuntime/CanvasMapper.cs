using UnityEngine;
using UstacaEller.Core.Manifest;

namespace UstacaEller.SceneRuntime
{
    /// <summary>
    /// Converts manifest coordinates into world space.
    ///
    /// Manifests use the convention art is drawn in: origin top-left, y growing
    /// downward, measured in design pixels. Unity uses world units with y growing
    /// upward and the origin wherever you put it. Keeping the flip in one place means
    /// a scene author never has to think about world units, and nothing else in the
    /// runtime has to remember which way up the numbers are.
    /// </summary>
    public readonly struct CanvasMapper
    {
        public const float DefaultPixelsPerUnit = 100f;

        private readonly float _width;
        private readonly float _height;

        public CanvasMapper(SceneCanvas canvas, float pixelsPerUnit = DefaultPixelsPerUnit)
        {
            _width = canvas.Width;
            _height = canvas.Height;
            PixelsPerUnit = pixelsPerUnit;
        }

        public float PixelsPerUnit { get; }

        public float DesignAspect => _width / _height;

        /// <summary>Orthographic size that fits the canvas height exactly.</summary>
        public float OrthographicSize => _height / (2f * PixelsPerUnit);

        /// <summary>
        /// Orthographic size that keeps the whole canvas on screen at the given aspect,
        /// letterboxing rather than cropping.
        ///
        /// Fitting height alone crops the sides on any device narrower than the design
        /// resolution, and a prop that is off-screen is one a child cannot reach — worse
        /// than one that looks slightly small.
        /// </summary>
        public float OrthographicSizeFor(float screenAspect)
        {
            if (screenAspect <= 0f) return OrthographicSize;

            return screenAspect < DesignAspect
                ? OrthographicSize * (DesignAspect / screenAspect)
                : OrthographicSize;
        }

        public Vector3 ToWorld(float canvasX, float canvasY, float z = 0f) => new Vector3(
            (canvasX - _width * 0.5f) / PixelsPerUnit,
            (_height * 0.5f - canvasY) / PixelsPerUnit,
            z);

        public Vector3 ToWorld(ObjectTransform transform, float z = 0f) => ToWorld(transform.X, transform.Y, z);

        public Vector2 ToCanvas(Vector3 world) => new Vector2(
            world.x * PixelsPerUnit + _width * 0.5f,
            _height * 0.5f - world.y * PixelsPerUnit);

        /// <summary>Converts a length. Unlike a position this carries no origin or flip.</summary>
        public float ToWorldLength(float canvasLength) => canvasLength / PixelsPerUnit;
    }
}
