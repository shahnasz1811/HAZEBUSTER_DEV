using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Follows the player with a SLANTED ORTHOGRAPHIC camera (the "Hell Maiden" look), or with the
    /// old flat top-down camera when Slanted View is off.
    ///
    /// SLANTED VIEW: the camera stays orthographic (no perspective shrinking) but is tilted back by
    /// Tilt Angle and placed Camera Distance away from the point it looks at. The game itself is
    /// unchanged and still plays on the flat XY plane; the ground just appears at a slant. 0 degrees
    /// is straight down (same as the old view), around 50 looks like a 3/4 "isometric" game, and
    /// 60 is a true 2:1 isometric ground.
    ///
    /// The camera follows the point on the GROUND under the player, then steps back along its own
    /// view direction. SmoothDamp removes camera micro-stutter and sprite jitter.
    ///
    /// SETUP: nothing. If the main camera has no CameraFollow, one is added when the scene loads
    /// (see SlantedPerspective). HazeBuster > Camera > Apply Slanted Orthographic View previews it
    /// in the Scene without pressing Play.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // run before other scripts' Awake so they see the right view
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The player transform to follow. Will auto-find tag 'Player' if left empty.")]
        public Transform target;

        [Header("Slanted Perspective (Hell Maiden style)")]
        [Tooltip("On: tilted orthographic camera with upright sprites. Off: the old flat top-down camera.")]
        public bool slantedView = true;

        [Tooltip("Tilt away from straight-down, in degrees. 0 = flat top-down, ~35 = gentle slant (default), ~50 = 3/4 view, 60 = 2:1 isometric ground.")]
        [Range(0f, 75f)]
        public float tiltAngle = 35f;

        [Tooltip("How far the camera sits from the point it looks at. Only needs to be big enough to keep the scene inside the clip planes.")]
        public float cameraDistance = 20f;

        [Header("Follow Settings")]
        [Tooltip("Smooth follow delay (0.08 - 0.15 is ideal). Set to 0 for instant camera lock.")]
        public float smoothTime = 0.1f;

        [Tooltip("Used only when Slanted View is off.")]
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

        // The offset from the look-at point to the camera, and the settings it was built from.
        private Vector3 activeOffset = new Vector3(0f, 0f, -10f);
        private bool appliedSlanted;
        private float appliedTilt = -1f;
        private float appliedDistance = -1f;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            ApplyPerspective();
        }

        private void Start()
        {
            FindPlayer();
            if (target != null)
            {
                transform.position = GetFocusPoint() + activeOffset;
            }
        }

        /// <summary>
        /// Applies Slanted View / Tilt Angle / Camera Distance to the camera. Runs on Awake, and again
        /// by itself if you change one of them while the game is running.
        /// </summary>
        public void ApplyPerspective()
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }

            float tilt = slantedView ? Mathf.Clamp(tiltAngle, 0f, SlantedPerspective.MaxTilt) : 0f;
            float distance = Mathf.Max(2f, cameraDistance);

            SlantedPerspective.SetTilt(tilt);
            SlantedPerspective.UprightSprites = slantedView;

            if (slantedView)
            {
                Quaternion rotation = SlantedPerspective.CameraRotation;
                transform.rotation = rotation;

                // The camera sits behind the point it looks at, along its own view direction.
                activeOffset = rotation * (Vector3.back * distance);

                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.nearClipPlane = 0.1f;
                    cam.farClipPlane = Mathf.Max(cam.farClipPlane, distance + 100f);
                }
            }
            else
            {
                transform.rotation = Quaternion.identity;
                activeOffset = offset;
            }

            appliedSlanted = slantedView;
            appliedTilt = tiltAngle;
            appliedDistance = cameraDistance;
        }

        /// <summary>
        /// Applies the view and puts the camera straight onto its target (or the world origin if there
        /// is none). Used by the editor menu to preview the view without pressing Play.
        /// </summary>
        public void SnapToTarget()
        {
            ApplyPerspective();
            FindPlayer();

            Vector3 focus = target != null ? GetFocusPoint() : Vector3.zero;
            transform.position = focus + activeOffset;
            currentVelocity = Vector3.zero;
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
        /// The point the camera looks at: the target on the ground, kept inside the arena or the
        /// legacy bounds when those are switched on.
        /// </summary>
        private Vector3 GetFocusPoint()
        {
            Vector3 focus = target.position;
            if (slantedView)
            {
                focus.z = 0f; // the look-at point always lies on the ground plane
            }

            if (useBounds)
            {
                focus.x = Mathf.Clamp(focus.x, minBounds.x, maxBounds.x);
                focus.y = Mathf.Clamp(focus.y, minBounds.y, maxBounds.y);
            }

            if (clampToArena && ArenaBounds.enabled)
            {
                focus = ClampToArena(focus);
            }

            return focus;
        }

        /// <summary>
        /// Keeps the whole visible ground inside the arena. If the view is bigger than the arena on
        /// one axis, the view centres on the arena on that axis instead.
        /// </summary>
        private Vector3 ClampToArena(Vector3 p)
        {
            Vector2 half = SlantedPerspective.VisibleHalfExtents(cam);

            float rangeX = ArenaBounds.halfSize - half.x;
            float rangeY = ArenaBounds.halfSize - half.y;

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
            // Settings changed in the Inspector while playing: rebuild the view.
            if (slantedView != appliedSlanted ||
                !Mathf.Approximately(tiltAngle, appliedTilt) ||
                !Mathf.Approximately(cameraDistance, appliedDistance))
            {
                ApplyPerspective();
            }

            if (target == null)
            {
                FindPlayer();
                return;
            }

            Vector3 targetPosition = GetFocusPoint() + activeOffset;

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
