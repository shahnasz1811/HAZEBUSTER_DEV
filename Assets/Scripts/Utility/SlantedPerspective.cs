using UnityEngine;
using UnityEngine.SceneManagement;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Shared maths and helpers for the slanted orthographic view (the "Hell Maiden" look).
    ///
    /// HOW THE VIEW WORKS
    /// Gameplay is untouched: everything still lives on the flat XY plane (z = 0) and uses
    /// Rigidbody2D / Collider2D. Only the camera changes. CameraFollow tilts an ORTHOGRAPHIC camera
    /// back by TiltDegrees, so the ground is seen at a slant (squashed vertically by cos(tilt)) with
    /// no perspective shrinking. Characters, enemies, pickups and props are drawn "upright" with the
    /// HazeBuster/Sprite Billboard shader, so they are not squashed together with the ground.
    ///
    /// Everything that used to assume "screen up = world up" goes through the helpers below:
    /// mouse aiming (ScreenToGround), how much ground is visible (VisibleHalfExtents) and which
    /// ground point is in the middle of the screen (ViewCenterOnGround).
    /// </summary>
    public static class SlantedPerspective
    {
        public const float MaxTilt = 75f;

        private const string ShaderResourceName = "HazeBusterSpriteBillboard";
        private const string ShaderName = "HazeBuster/Sprite Billboard";

        private static float tilt;
        private static Material billboardMaterial;
        private static Material flatSpriteMaterial;
        private static bool shaderLookupDone;

        /// <summary>
        /// While true, MakeUpright() draws sprites upright. CameraFollow turns this off when its
        /// Slanted View option is off, so the flat 2D look still works.
        /// </summary>
        public static bool UprightSprites = true;

        /// <summary>Camera tilt away from straight-down, in degrees. 0 = the old flat 2D view.</summary>
        public static float TiltDegrees
        {
            get { return tilt; }
        }

        public static bool IsSlanted
        {
            get { return tilt > 0.5f; }
        }

        /// <summary>The rotation the camera must have for this tilt (it looks "up" the world Y axis).</summary>
        public static Quaternion CameraRotation
        {
            get { return Quaternion.Euler(-tilt, 0f, 0f); }
        }

        public static void SetTilt(float degrees)
        {
            tilt = Mathf.Clamp(degrees, 0f, MaxTilt);
        }

        // ---------------------------------------------------------------- ground maths

        /// <summary>
        /// Half the width and half the height of the GROUND (z = 0) that the camera can see.
        /// A slanted camera sees more ground vertically: orthographicSize / cos(tilt).
        /// Read from the camera's real rotation, so it is right even before the tilt is applied.
        /// </summary>
        public static Vector2 VisibleHalfExtents(Camera cam)
        {
            if (cam == null || !cam.orthographic)
            {
                return new Vector2(12f, 8f);
            }

            float facing = Mathf.Max(0.25f, Mathf.Abs(cam.transform.forward.z));
            return new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize / facing);
        }

        /// <summary>
        /// The ground point (z = 0) in the exact middle of the screen. For a slanted camera this is
        /// not the camera's own x/y position, because the camera sits back from what it looks at.
        /// </summary>
        public static Vector2 ViewCenterOnGround(Camera cam)
        {
            Transform t = cam.transform;
            Vector3 p = t.position;
            Vector3 f = t.forward;

            if (Mathf.Abs(f.z) < 0.0001f)
            {
                return new Vector2(p.x, p.y);
            }

            float distance = -p.z / f.z;
            return new Vector2(p.x + (f.x * distance), p.y + (f.y * distance));
        }

        /// <summary>
        /// Where the mouse points on the ground. Works for a flat or a slanted camera.
        /// Camera.ScreenToWorldPoint cannot be used for aiming any more: with a tilted camera it
        /// returns a point on the camera's own plane, not on the ground.
        /// </summary>
        public static Vector2 ScreenToGround(Camera cam, Vector3 screenPosition, Vector2 fallback)
        {
            if (cam == null)
            {
                return fallback;
            }

            Ray ray = cam.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
            {
                return fallback;
            }

            // Intersect the ray with the ground plane z = 0.
            float distance = -ray.origin.z / ray.direction.z;
            Vector3 hit = ray.origin + (ray.direction * distance);
            return new Vector2(hit.x, hit.y);
        }

        // ---------------------------------------------------------------- upright sprites

        /// <summary>The shared billboard material, or null if the shader could not be found.</summary>
        public static Material BillboardMaterial
        {
            get
            {
                if (billboardMaterial == null && !shaderLookupDone)
                {
                    shaderLookupDone = true;

                    Shader shader = Resources.Load<Shader>(ShaderResourceName);
                    if (shader == null)
                    {
                        shader = Shader.Find(ShaderName);
                    }

                    if (shader != null && shader.isSupported)
                    {
                        billboardMaterial = new Material(shader);
                        billboardMaterial.name = "HazeBuster Sprite Billboard (runtime)";
                        billboardMaterial.hideFlags = HideFlags.HideAndDontSave;
                    }
                    else
                    {
                        Debug.LogWarning("[HAZEBUSTER] Sprite Billboard shader not found (Assets/Shaders/Resources/HazeBusterSpriteBillboard.shader). Sprites will be squashed along with the ground until it compiles.");
                    }
                }
                return billboardMaterial;
            }
        }

        /// <summary>Draws every SpriteRenderer on this object and its children upright.</summary>
        public static void MakeUpright(GameObject root)
        {
            if (root == null || !UprightSprites) return;

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SetUpright(renderers[i], true);
            }
        }

        /// <summary>
        /// Switches one renderer between "upright" (stands up facing the camera) and "flat"
        /// (lies on the ground like a decal). Used for pooled renderers that change role.
        /// </summary>
        public static void SetUpright(SpriteRenderer renderer, bool upright)
        {
            if (renderer == null) return;

            Material billboard = BillboardMaterial;
            if (billboard == null) return;

            if (upright)
            {
                if (UprightSprites && renderer.sharedMaterial != billboard)
                {
                    renderer.sharedMaterial = billboard;
                }
            }
            else if (renderer.sharedMaterial == billboard)
            {
                renderer.sharedMaterial = GetFlatSpriteMaterial();
            }
        }

        private static Material GetFlatSpriteMaterial()
        {
            if (flatSpriteMaterial == null)
            {
                // A fresh SpriteRenderer starts with Unity's default sprite material.
                GameObject probe = new GameObject("SpriteMaterialProbe");
                probe.hideFlags = HideFlags.HideAndDontSave;
                flatSpriteMaterial = probe.AddComponent<SpriteRenderer>().sharedMaterial;
                Object.Destroy(probe);
            }
            return flatSpriteMaterial;
        }

        // ---------------------------------------------------------------- setup

        // Static fields survive a play session when Domain Reload is switched off, so reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            tilt = 0f;
            UprightSprites = true;
            billboardMaterial = null;
            flatSpriteMaterial = null;
            shaderLookupDone = false;
        }

        // Makes sure the main camera has a CameraFollow, so the slanted view works with no setup.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureCameraRig();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureCameraRig();
        }

        private static void EnsureCameraRig()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.GetComponent<CameraFollow>() == null)
            {
                mainCamera.gameObject.AddComponent<CameraFollow>();
            }
        }
    }
}
