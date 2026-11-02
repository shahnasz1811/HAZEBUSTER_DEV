using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Smoothly follows the player character across the peatland environment.
    /// Uses SmoothDamp to eliminate camera micro-stutter and sprite jitter!
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The player transform to follow. Will auto-find tag 'Player' if left empty.")]
        public Transform target;

        [Header("Follow Settings")]
        [Tooltip("Smooth follow delay (0.08 - 0.15 is ideal). Set to 0 for instant camera lock.")]
        public float smoothTime = 0.1f;
        public Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Arena Clamp")]
        [Tooltip("Keeps the camera view inside ArenaBounds so nothing past the arena edge is shown.")]
        public bool clampToArena = true;

        [Header("Fixed Bounds (legacy, off by default)")]
        public bool useBounds = false;
        public Vector2 minBounds = new Vector2(-25f, -25f);
        public Vector2 maxBounds = new Vector2(25f, 25f);

        private Vector3 currentVelocity = Vector3.zero;
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            FindPlayer();
            if (target != null)
            {
                Vector3 startPos = target.position + offset;
                transform.position = (clampToArena && ArenaBounds.enabled) ? ClampToArena(startPos) : startPos;
            }
        }

        private void FindPlayer()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        /// <summary>
        /// Keeps the whole camera view inside the arena. If the view is bigger than the arena on
        /// one axis, the camera centres on the arena on that axis instead.
        /// </summary>
        private Vector3 ClampToArena(Vector3 p)
        {
            float halfH = (cam != null && cam.orthographic) ? cam.orthographicSize : 0f;
            float halfW = (cam != null && cam.orthographic) ? halfH * cam.aspect : 0f;

            float rangeX = ArenaBounds.halfSize - halfW;
            float rangeY = ArenaBounds.halfSize - halfH;

            p.x = rangeX > 0f
                ? Mathf.Clamp(p.x, ArenaBounds.center.x - rangeX, ArenaBounds.center.x + rangeX)
                : ArenaBounds.center.x;
            p.y = rangeY > 0f
                ? Mathf.Clamp(p.y, ArenaBounds.center.y - rangeY, ArenaBounds.center.y + rangeY)
                : ArenaBounds.center.y;
            return p;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindPlayer();
                return;
            }

            Vector3 targetPosition = target.position + offset;

            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
            }

            if (clampToArena && ArenaBounds.enabled)
            {
                targetPosition = ClampToArena(targetPosition);
            }

            if (smoothTime > 0.001f)
            {
                // SmoothDamp eliminates the micro-stutter caused by Lerp
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }
}
