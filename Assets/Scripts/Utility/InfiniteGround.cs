using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Endless ground, like Vampire Survivors.
    ///
    /// The ground is ONE tiled sprite that follows the camera. It only moves in whole-tile steps,
    /// so the tile pattern stays locked to a fixed grid in the world: the ground you see at any
    /// world position is always the same, and you never see the sprite move. It resizes itself to
    /// cover the camera view, so it stays small and cheap even when the tiles are tiny.
    ///
    /// What makes places feel different (rocks, logs, scorch marks) is not here. That is the job of
    /// EnvironmentStreamer, which remembers each place of the world.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InfiniteGround : MonoBehaviour
    {
        [Header("Follow")]
        [Tooltip("The camera whose view the ground must cover. Leave empty to use the main camera.")]
        public Camera viewCamera;

        [Tooltip("Only used when there is no camera.")]
        public Transform target;

        [Header("Tile Settings")]
        [Tooltip("Filled in automatically from the sprite (pixels / pixels per unit).")]
        public float tileSize = 1f;

        [Tooltip("The ground jumps once the view drifts this many tiles away from its centre.")]
        public int tilesPerChunk = 16;

        private SpriteRenderer spriteRenderer;
        private float lastOrthoSize = -1f;
        private float lastAspect = -1f;

        private float ChunkStep
        {
            get { return tileSize * Mathf.Max(1, tilesPerChunk); }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;

            // Done in Awake so EnvironmentStreamer can read a correct tileSize in its own Start().
            if (spriteRenderer.sprite != null)
            {
                float width = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
                if (width > 0.0001f) tileSize = width;
            }
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        }

        private void Start()
        {
            Transform reference = GetReference();
            if (reference != null)
            {
                SnapTo(reference.position);
            }
            ResizeToView();
        }

        private void LateUpdate()
        {
            Transform reference = GetReference();
            if (reference == null) return;

            // Camera zoom or window shape changed: cover the new view.
            if (viewCamera != null && viewCamera.orthographic &&
                (!Mathf.Approximately(viewCamera.orthographicSize, lastOrthoSize) ||
                 !Mathf.Approximately(viewCamera.aspect, lastAspect)))
            {
                ResizeToView();
            }

            float step = ChunkStep;
            Vector3 pos = transform.position;
            Vector3 refPos = reference.position;

            // Jump only when the view has drifted a whole chunk away from the ground's centre.
            if (Mathf.Abs(refPos.x - pos.x) >= step || Mathf.Abs(refPos.y - pos.y) >= step)
            {
                SnapTo(refPos);
            }
        }

        private Transform GetReference()
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }
            if (viewCamera != null)
            {
                return viewCamera.transform;
            }
            return target != null ? target : PlayerLocator.PlayerTransform;
        }

        /// <summary>
        /// Moves the ground to the nearest chunk step, measured from the world origin
        /// (absolute, so tiny rounding errors never add up).
        /// </summary>
        private void SnapTo(Vector3 worldPoint)
        {
            float step = ChunkStep;
            transform.position = new Vector3(
                Mathf.Round(worldPoint.x / step) * step,
                Mathf.Round(worldPoint.y / step) * step,
                transform.position.z);
        }

        /// <summary>
        /// Sizes the sprite to the camera view plus the distance it can drift before it jumps.
        /// The size is a whole, even number of tiles so the pattern stays on the world grid.
        /// </summary>
        private void ResizeToView()
        {
            if (spriteRenderer == null || tileSize <= 0.0001f) return;

            float halfHeight = 6f;
            float halfWidth = 11f;
            if (viewCamera != null && viewCamera.orthographic)
            {
                halfHeight = viewCamera.orthographicSize;
                halfWidth = halfHeight * viewCamera.aspect;
                lastOrthoSize = viewCamera.orthographicSize;
                lastAspect = viewCamera.aspect;
            }

            float margin = ChunkStep + (tileSize * 4f);
            int halfTilesX = Mathf.CeilToInt((halfWidth + margin) / tileSize);
            int halfTilesY = Mathf.CeilToInt((halfHeight + margin) / tileSize);

            spriteRenderer.size = new Vector2(halfTilesX * 2f * tileSize, halfTilesY * 2f * tileSize);
        }
    }
}
