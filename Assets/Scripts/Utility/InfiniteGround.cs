using UnityEngine;
using HazeBuster.Art;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Endless ground, like Vampire Survivors.
    ///
    /// A single flat surface follows the middle of the camera view and draws a repeating ground
    /// picture. The picture is placed by WORLD position in the shader, not by the surface, so it
    /// never slides, snaps or pops however the surface moves. (The old version moved a tiled sprite
    /// in chunk steps, which could make patches of ground jump and tiny pixels shimmer.)
    ///
    /// GROUND PICTURE, first match wins:
    ///   1. Ground Texture on this component
    ///   2. Ground Texture in the Art Library (HazeBuster > Art > Set Up Replaceable Art)
    ///   3. a generated, soft, painted-looking peat placeholder
    ///
    /// Use a seamless (tileable) picture. HazeBuster > Art > Prepare Selected Texture As Ground sets
    /// the import settings it needs (repeat, mip maps, smooth filtering).
    ///
    /// SIZE: one repeat of the picture covers a number of character heights (Art Library > Ground
    /// Tile In Characters), so the ground stays in proportion with the characters. Fill in Tile
    /// World Size below to use exact world units instead.
    ///
    /// The old tiled sprite on this object is hidden. The surface itself only exists while playing.
    ///
    /// What makes places feel different (rocks, logs, scorch marks) is not here. That is the job of
    /// EnvironmentStreamer, which remembers each place of the world.
    /// </summary>
    [DefaultExecutionOrder(1000)] // after CameraFollow, so the surface uses this frame's camera position
    public class InfiniteGround : MonoBehaviour
    {
        private const string ShaderResourceName = "HazeBusterEndlessGround";
        private const string ShaderName = "HazeBuster/Endless Ground";

        [Header("Follow")]
        [Tooltip("The camera whose view the ground must cover. Leave empty to use the main camera.")]
        public Camera viewCamera;

        [Tooltip("Only used when there is no camera.")]
        public Transform target;

        [Header("Ground picture")]
        [Tooltip("Your seamless ground picture. Leave empty to use the Art Library's, or a generated placeholder.")]
        public Texture2D groundTexture;

        [Tooltip("World units covered by one repeat of the picture. 0 = automatic: Ground Tile In Characters (Art Library) times the character height.")]
        public float tileWorldSize = 0f;

        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int TileSizeId = Shader.PropertyToID("_TileSize");
        private static readonly int VariationId = Shader.PropertyToID("_Variation");
        private static readonly int VariationScaleId = Shader.PropertyToID("_VariationScale");

        private SpriteRenderer legacySprite;
        private Material material;
        private Texture2D generatedTexture;
        private Mesh mesh;
        private Transform surface;
        private MeshRenderer surfaceRenderer;
        private float waited;

        private void Awake()
        {
            // The old version drew the ground as a tiled sprite. Hide it.
            legacySprite = GetComponent<SpriteRenderer>();
            if (legacySprite != null)
            {
                legacySprite.enabled = false;
            }

            Shader shader = Resources.Load<Shader>(ShaderResourceName);
            if (shader == null)
            {
                shader = Shader.Find(ShaderName);
            }
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("[HAZEBUSTER] Endless Ground shader not found (Assets/Shaders/Resources/HazeBusterEndlessGround.shader). Check the Console for a shader error. The old ground sprite is shown instead.");
                if (legacySprite != null)
                {
                    legacySprite.enabled = true;
                }
                enabled = false;
                return;
            }

            Texture2D texture = groundTexture;
            if (texture == null && ArtLibrary.Instance != null)
            {
                texture = ArtLibrary.Instance.groundTexture;
            }
            if (texture == null)
            {
                generatedTexture = BuildPeatTexture();
                texture = generatedTexture;
            }

            // The picture must repeat. (The shader repeats it by position, so it needs Repeat wrap.)
            texture.wrapMode = TextureWrapMode.Repeat;
            if (texture.anisoLevel < 4)
            {
                texture.anisoLevel = 8; // keeps the ground sharp where it is seen at a slant
            }

            material = new Material(shader);
            material.name = "HazeBuster Endless Ground (runtime)";
            material.hideFlags = HideFlags.HideAndDontSave;
            material.mainTexture = texture;

            BuildSurface();
        }

        private void OnDestroy()
        {
            if (surface != null) Destroy(surface.gameObject);
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
            if (generatedTexture != null) Destroy(generatedTexture);
        }

        private void LateUpdate()
        {
            if (surface == null) return;

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            Vector2 center;
            Vector2 half;
            if (viewCamera != null && viewCamera.orthographic)
            {
                // The ground point in the middle of the screen, and how much ground the (slanted) view shows.
                center = SlantedPerspective.ViewCenterOnGround(viewCamera);
                half = SlantedPerspective.VisibleHalfExtents(viewCamera);
            }
            else
            {
                Transform fallback = target != null ? target : PlayerLocator.PlayerTransform;
                center = fallback != null ? (Vector2)fallback.position : Vector2.zero;
                half = new Vector2(14f, 10f);
            }

            // A margin so a fast camera never shows the edge of the surface.
            float margin = 2f + (Mathf.Max(half.x, half.y) * 0.1f);
            surface.position = new Vector3(center.x, center.y, 0f);
            surface.localScale = new Vector3((half.x + margin) * 2f, (half.y + margin) * 2f, 1f);

            // Wait until the player has been measured, so the ground size is right from the first frame.
            waited += Time.unscaledDeltaTime;
            bool ready = tileWorldSize > 0.0001f || WorldScale.IsReady || waited > 1f;
            if (surfaceRenderer.enabled != ready)
            {
                surfaceRenderer.enabled = ready;
            }

            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            ArtLibrary library = ArtLibrary.Instance;

            float tile = tileWorldSize;
            if (tile <= 0.0001f)
            {
                float characters = library != null ? library.groundTileInCharacters : 10f;
                tile = WorldScale.CharacterHeight * Mathf.Max(0.5f, characters);
            }

            material.SetFloat(TileSizeId, tile);
            material.SetColor(TintId, library != null ? library.groundTint : Color.white);
            material.SetFloat(VariationId, library != null ? library.groundVariation : 0.3f);
            material.SetFloat(VariationScaleId, tile * 3f);
        }

        // A unit square lying on the ground (z = 0). It is scaled and moved every frame.
        private void BuildSurface()
        {
            mesh = new Mesh();
            mesh.name = "Endless Ground Quad";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();

            GameObject go = new GameObject("Endless Ground Surface");
            surface = go.transform; // not parented, so this object's own transform can never distort it

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            surfaceRenderer = go.AddComponent<MeshRenderer>();
            surfaceRenderer.sharedMaterial = material;
            surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            surfaceRenderer.receiveShadows = false;
            surfaceRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            surfaceRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            surfaceRenderer.sortingOrder = -100;
            surfaceRenderer.enabled = false; // shown once the world scale is known
        }

        // ---------------------------------------------------------------- placeholder ground

        /// <summary>
        /// A soft, seamless, painted-looking peat picture made from layered noise. It repeats without
        /// seams and has mip maps, so it stays calm at a slant (no shimmering).
        /// </summary>
        private static Texture2D BuildPeatTexture()
        {
            const int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = "Peat Ground (generated)";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.anisoLevel = 8;

            Color dark = new Color(0.12f, 0.09f, 0.07f, 1f);
            Color mid = new Color(0.21f, 0.15f, 0.11f, 1f);
            Color dry = new Color(0.30f, 0.22f, 0.15f, 1f);
            Color moss = new Color(0.17f, 0.23f, 0.11f, 1f);

            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;

                    float broad = TileNoise(u, v, 4, 11);
                    float medium = TileNoise(u, v, 12, 23);
                    float fine = TileNoise(u, v, 40, 37);
                    float n = (broad * 0.5f) + (medium * 0.32f) + (fine * 0.18f);

                    Color c = Color.Lerp(dark, mid, Mathf.Clamp01((n - 0.25f) * 1.8f));

                    float dryAmount = Mathf.Clamp01((TileNoise(u, v, 7, 51) - 0.6f) * 3f);
                    c = Color.Lerp(c, dry, dryAmount * 0.6f);

                    float mossAmount = Mathf.Clamp01((TileNoise(u, v, 5, 77) - 0.58f) * 3.2f);
                    c = Color.Lerp(c, moss, mossAmount * 0.7f);

                    c.a = 1f;
                    px[(y * size) + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply(true); // true = build the mip maps
            return tex;
        }

        // Smooth value noise on a grid of `frequency` cells that wraps around, so it tiles.
        private static float TileNoise(float u, float v, int frequency, int seed)
        {
            float x = u * frequency;
            float y = v * frequency;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - (2f * fx));
            fy = fy * fy * (3f - (2f * fy));

            int x1 = (x0 + 1) % frequency;
            int y1 = (y0 + 1) % frequency;
            x0 = x0 % frequency;
            y0 = y0 % frequency;

            float a = LatticeValue(x0, y0, seed);
            float b = LatticeValue(x1, y0, seed);
            float c = LatticeValue(x0, y1, seed);
            float d = LatticeValue(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float LatticeValue(int x, int y, int seed)
        {
            unchecked
            {
                int h = (x * 374761393) + (y * 668265263) + (seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= (h >> 16);
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
