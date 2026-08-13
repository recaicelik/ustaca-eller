using UnityEngine;

namespace UstacaEller.SceneRuntime
{
    /// <summary>
    /// Drops a manifest-defined scene into a running game.
    ///
    /// This is the whole of the play surface for now: no scene asset holds any props,
    /// no prefab knows what a kitchen contains. Everything comes from
    /// content/scenes/&lt;id&gt;/manifest.json at load time, which is what keeps the
    /// promise that adding a scene never means touching C#.
    /// </summary>
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [SerializeField]
        private string sceneId = "kitchen";

        [SerializeField]
        private Camera sceneCamera;

        [SerializeField]
        private Color backgroundColour = new Color(0.94f, 0.92f, 0.88f);

        public BuiltScene Current { get; private set; }

        private void Start()
        {
            Load(sceneId);
        }

        public BuiltScene Load(string id)
        {
            if (Current?.Root != null) Destroy(Current.Root);

            Current = new SceneBuilder().Build(new SceneCatalog().Load(id));
            FrameCamera(sceneCamera != null ? sceneCamera : Camera.main, Current);

            return Current;
        }

        /// <summary>
        /// Fits the camera to the scene's design resolution. A scene author works in
        /// canvas pixels and should never have to reason about orthographic size.
        /// </summary>
        public void FrameCamera(Camera camera, BuiltScene scene)
        {
            if (camera == null) return;

            camera.orthographic = true;
            camera.orthographicSize = scene.Mapper.OrthographicSize;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColour;
        }
    }
}
