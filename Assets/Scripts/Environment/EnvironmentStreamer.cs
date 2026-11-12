using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HazeBuster.Art;
using HazeBuster.Utility;

namespace HazeBuster.Environment
{
    /// <summary>What the player left behind on the ground. The world remembers it.</summary>
    public enum EnvironmentMarkType
    {
        Restored,   // a fire was put out here: damp, recovering peat
        Scorched    // a fire burned itself out here: charred peat
    }

    /// <summary>
    /// Builds the endless peatland around the camera, one square chunk at a time, and remembers it.
    ///
    /// TWO KINDS OF MEMORY:
    /// 1. The layout. Every chunk of the world is generated from (world seed, chunk x, chunk y) and
    ///    nothing else, so a place always looks the same when you walk back to it. Chunks that are
    ///    far from the camera are simply removed from the scene and cost nothing.
    /// 2. The changes. When something calls EnvironmentStreamer.AddMark (a fire is put out, a fire
    ///    burns out), the mark is stored for its chunk and drawn again every time the chunk loads.
    ///
    /// The ground tiles themselves come from InfiniteGround. This only adds the props and marks.
    ///
    /// SETUP: nothing is needed. If the scene has no EnvironmentStreamer, one is created when the
    /// scene loads, with simple generated placeholder props. To use your own art, add this component
    /// to any object (for example _GameManagers) and fill in the Decor Sprites list.
    /// </summary>
    public class EnvironmentStreamer : MonoBehaviour
    {
        public static EnvironmentStreamer Instance { get; private set; }

        [Header("World")]
        [Tooltip("On: every run gets a different world. Off: the World Seed below is used, so every run is the same world.")]
        public bool randomSeedEachRun = true;
        public int worldSeed = 20261102;

        [Tooltip("Side of one chunk, in world units.")]
        public float chunkSize = 10f;

        [Tooltip("How many extra chunks to keep loaded beyond the edge of the camera view.")]
        public int loadMarginChunks = 1;

        [Header("Decor (the look of the peatland)")]
        [Tooltip("Props scattered over the world. Leave empty to use the Art Library's props, or generated placeholders.")]
        public Sprite[] decorSprites;
        public int minDecorPerChunk = 3;
        public int maxDecorPerChunk = 7;

        [Tooltip("Smallest and largest prop HEIGHT, counted in character heights (the player's height). Props stay in proportion with the characters whatever size your art has.")]
        public Vector2 decorHeightInCharacters = new Vector2(0.7f, 2.5f);
        public int decorSortingOrder = -5;

        [Header("Memory (changes the player makes)")]
        [Tooltip("Leave empty to use the Art Library's marks, or generated placeholders.")]
        public Sprite restoredMarkSprite;
        public Sprite scorchedMarkSprite;
        [Tooltip("Width of a ground mark, counted in character heights.")]
        public float markSizeInCharacters = 4f;

        [Tooltip("Marks kept per chunk. When full, the oldest one fades from memory.")]
        public int maxMarksPerChunk = 40;
        public int markSortingOrder = -8;


        private struct Mark
        {
            public Vector2 position;
            public EnvironmentMarkType type;
            public float rotation;
        }

        // Memory: every mark ever made, by chunk. Lives as long as the scene.
        private readonly Dictionary<Vector2Int, List<Mark>> marks = new Dictionary<Vector2Int, List<Mark>>();

        // Chunks that are currently drawn, with the renderers they use.
        private readonly Dictionary<Vector2Int, List<SpriteRenderer>> loaded = new Dictionary<Vector2Int, List<SpriteRenderer>>();

        private readonly Stack<SpriteRenderer> idle = new Stack<SpriteRenderer>();
        private readonly List<Vector2Int> removeBuffer = new List<Vector2Int>();
        private readonly List<Object> generated = new List<Object>();

        private Camera cam;
        private Vector2Int lastMin;
        private Vector2Int lastMax;
        private bool hasRange;

        // ---------------------------------------------------------------- creation

        // Creates a streamer when the scene has none, so the endless world works with no setup.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureExists();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureExists();
        }

        private static void EnsureExists()
        {
            if (FindFirstObjectByType<EnvironmentStreamer>() == null)
            {
                new GameObject("_EnvironmentStreamer").AddComponent<EnvironmentStreamer>();
            }
        }

        private void Awake()
        {
            Instance = this;

            // The chunk objects are children, so this object must not move, turn or scale them.
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if (randomSeedEachRun)
            {
                worldSeed = Random.Range(1, int.MaxValue);
            }

            BuildPlaceholderSprites();
        }


        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            for (int i = 0; i < generated.Count; i++)
            {
                if (generated[i] != null) Destroy(generated[i]);
            }
        }

        // ---------------------------------------------------------------- streaming

        private void LateUpdate()
        {
            if (!WorldScale.IsReady || chunkSize <= 0.01f) return;

            if (cam == null) cam = Camera.main;

            Vector2 center;
            float halfWidth;
            float halfHeight;
            if (cam != null && cam.orthographic)
            {
                // The ground point in the middle of the screen (not the camera's own position: a slanted camera sits back from it).
                center = SlantedPerspective.ViewCenterOnGround(cam);
                // A slanted camera sees more ground vertically than orthographicSize alone.
                Vector2 visible = SlantedPerspective.VisibleHalfExtents(cam);
                halfHeight = visible.y;
                halfWidth = visible.x;
            }
            else
            {
                Transform player = PlayerLocator.PlayerTransform;
                if (player == null) return;
                center = player.position;
                halfWidth = 12f;
                halfHeight = 8f;
            }

            float margin = chunkSize * Mathf.Max(0, loadMarginChunks);
            Vector2Int min = ToChunk(new Vector2(center.x - halfWidth - margin, center.y - halfHeight - margin));
            Vector2Int max = ToChunk(new Vector2(center.x + halfWidth + margin, center.y + halfHeight + margin));

            // Nothing to do while the camera stays inside the same block of chunks.
            if (hasRange && min == lastMin && max == lastMax) return;

            // Safety: a camera zoomed out absurdly far would try to build thousands of chunks.
            if ((max.x - min.x + 1) * (max.y - min.y + 1) > 600) return;

            hasRange = true;
            lastMin = min;
            lastMax = max;

            // Drop chunks that left the view.
            removeBuffer.Clear();
            foreach (KeyValuePair<Vector2Int, List<SpriteRenderer>> pair in loaded)
            {
                Vector2Int k = pair.Key;
                if (k.x < min.x || k.x > max.x || k.y < min.y || k.y > max.y)
                {
                    removeBuffer.Add(k);
                }
            }
            for (int i = 0; i < removeBuffer.Count; i++)
            {
                UnloadChunk(removeBuffer[i]);
            }

            // Build chunks that came into view.
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    Vector2Int key = new Vector2Int(x, y);
                    if (!loaded.ContainsKey(key))
                    {
                        LoadChunk(key);
                    }
                }
            }
        }

        private Vector2Int ToChunk(Vector2 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / chunkSize),
                Mathf.FloorToInt(worldPosition.y / chunkSize));
        }

        private void LoadChunk(Vector2Int key)
        {
            List<SpriteRenderer> used = new List<SpriteRenderer>();
            loaded[key] = used;

            // 1. The layout. A random generator seeded only by (world seed, chunk x, chunk y) gives
            //    the same props every time this chunk is built.
            if (decorSprites != null && decorSprites.Length > 0)
            {
                System.Random rng = new System.Random(HashChunk(worldSeed, key.x, key.y));
                int low = Mathf.Max(0, minDecorPerChunk);
                int high = Mathf.Max(low, maxDecorPerChunk);
                int count = rng.Next(low, high + 1);
                float originX = key.x * chunkSize;
                float originY = key.y * chunkSize;

                for (int i = 0; i < count; i++)
                {
                    // Every value is drawn even when the prop is skipped, so the rest of the
                    // chunk never changes because of one empty sprite slot.
                    float x = originX + ((float)rng.NextDouble() * chunkSize);
                    float y = originY + ((float)rng.NextDouble() * chunkSize);
                    Sprite sprite = decorSprites[rng.Next(decorSprites.Length)];
                    float heightInCharacters = Mathf.Lerp(decorHeightInCharacters.x, decorHeightInCharacters.y, (float)rng.NextDouble());
                    bool flip = rng.Next(2) == 0;
                    float shade = Mathf.Lerp(0.7f, 1f, (float)rng.NextDouble());

                    if (sprite == null) continue;

                    SpriteRenderer sr = Acquire();
                    Place(sr, FeetSprite(sprite), new Vector2(x, y), heightInCharacters * WorldScale.CharacterHeight, true, 0f, flip,
                        new Color(shade, shade, shade, 1f), decorSortingOrder);
                    SlantedPerspective.SetUpright(sr, true); // props stand up; ground marks stay flat
                    used.Add(sr);
                }
            }

            // 2. The changes. Whatever the player did in this chunk comes back exactly as it was.
            List<Mark> chunkMarks;
            if (marks.TryGetValue(key, out chunkMarks))
            {
                for (int i = 0; i < chunkMarks.Count; i++)
                {
                    Mark mark = chunkMarks[i];
                    Sprite sprite = mark.type == EnvironmentMarkType.Restored ? restoredMarkSprite : scorchedMarkSprite;
                    if (sprite == null) continue;

                    SpriteRenderer sr = Acquire();
                    Place(sr, sprite, mark.position, markSizeInCharacters * WorldScale.CharacterHeight, false, mark.rotation, false,
                        Color.white, markSortingOrder);
                    SlantedPerspective.SetUpright(sr, false);
                    used.Add(sr);
                }
            }
        }

        private void UnloadChunk(Vector2Int key)
        {
            List<SpriteRenderer> used;
            if (!loaded.TryGetValue(key, out used)) return;

            for (int i = 0; i < used.Count; i++)
            {
                Release(used[i]);
            }
            loaded.Remove(key);
        }

        // ---------------------------------------------------------------- memory

        /// <summary>
        /// Leaves a lasting mark on the world at this position. Does nothing if there is no streamer.
        /// </summary>
        public static void AddMark(Vector2 worldPosition, EnvironmentMarkType type)
        {
            if (Instance != null)
            {
                Instance.AddMarkInternal(worldPosition, type);
            }
        }

        private void AddMarkInternal(Vector2 position, EnvironmentMarkType type)
        {
            if (!WorldScale.IsReady) return;

            Vector2Int key = ToChunk(position);
            List<Mark> list;
            if (!marks.TryGetValue(key, out list))
            {
                list = new List<Mark>();
                marks[key] = list;
            }

            // A new mark covers an older one underneath it, so patches never pile up.
            float replaceRadius = markSizeInCharacters * WorldScale.CharacterHeight * 0.5f;
            float replaceSqr = replaceRadius * replaceRadius;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if ((list[i].position - position).sqrMagnitude <= replaceSqr)
                {
                    list.RemoveAt(i);
                }
            }

            Mark mark = new Mark();
            mark.position = position;
            mark.type = type;
            mark.rotation = Random.Range(0f, 360f); // stored, so the patch looks the same on every reload
            list.Add(mark);

            while (list.Count > Mathf.Max(1, maxMarksPerChunk))
            {
                list.RemoveAt(0);
            }

            // If this chunk is on screen right now, redraw it so the mark shows up immediately.
            if (loaded.ContainsKey(key))
            {
                UnloadChunk(key);
                LoadChunk(key);
            }
        }

        // ---------------------------------------------------------------- drawing helpers

        private SpriteRenderer Acquire()
        {
            while (idle.Count > 0)
            {
                SpriteRenderer reused = idle.Pop();
                if (reused != null) return reused;
            }

            GameObject go = new GameObject("Decor");
            go.transform.SetParent(transform, false);
            return go.AddComponent<SpriteRenderer>();
        }

        private void Release(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            idle.Push(sr);
        }

        private static void Place(SpriteRenderer sr, Sprite sprite, Vector2 position, float worldSize, bool sizeByHeight,
                                  float rotation, bool flipX, Color color, int sortingOrder)
        {
            sr.sprite = sprite;
            sr.flipX = flipX;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            Transform t = sr.transform;
            t.position = new Vector3(position.x, position.y, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, rotation);

            float spriteSize = sizeByHeight ? sprite.bounds.size.y : sprite.bounds.size.x;
            float scale = spriteSize > 0.0001f ? worldSize / spriteSize : 1f;
            t.localScale = new Vector3(scale, scale, 1f);

            sr.gameObject.SetActive(true);
        }

        // Props stand on the ground: the same picture, with its pivot at the bottom centre.
        private readonly Dictionary<Sprite, Sprite> feetSprites = new Dictionary<Sprite, Sprite>();

        private Sprite FeetSprite(Sprite source)
        {
            Sprite result;
            if (feetSprites.TryGetValue(source, out result) && result != null)
            {
                return result;
            }

            result = Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            result.name = source.name + " (feet)";
            feetSprites[source] = result;
            generated.Add(result);
            return result;
        }

        /// <summary>Mixes the world seed and the chunk coordinates into one random seed.</summary>
        private static int HashChunk(int seed, int x, int y)
        {
            unchecked
            {
                int h = seed;
                h = (h * 73856093) ^ (x * 19349663) ^ (y * 83492791);
                h ^= (h >> 13);
                h *= 1274126177;
                h ^= (h >> 16);
                return h;
            }
        }

        // ---------------------------------------------------------------- placeholder art

        private void BuildPlaceholderSprites()
        {
            ArtLibrary library = ArtLibrary.Instance;

            // Your own art first (this component, then the Art Library), generated placeholders last.
            if (decorSprites == null || decorSprites.Length == 0)
            {
                if (library != null && library.props != null && library.props.Length > 0)
                {
                    decorSprites = library.props;
                }
                else
                {
                    decorSprites = new Sprite[]
                    {
                        MakeSprite(0), // rock
                        MakeSprite(1), // charred log
                        MakeSprite(2)  // fern tuft
                    };
                }
            }
            if (restoredMarkSprite == null)
            {
                restoredMarkSprite = (library != null && library.restoredMark != null) ? library.restoredMark : MakeSprite(4);
            }
            if (scorchedMarkSprite == null)
            {
                scorchedMarkSprite = (library != null && library.scorchedMark != null) ? library.scorchedMark : MakeSprite(5);
            }
        }

        private Sprite MakeSprite(int kind)
        {
            const int size = 16;
            Color[] px = new Color[size * size]; // starts fully transparent

            switch (kind)
            {
                case 0: // rock
                    FillEllipse(px, size, 8f, 7f, 6.5f, 4.5f, new Color(0.30f, 0.27f, 0.25f, 1f));
                    FillEllipse(px, size, 7f, 8.5f, 3.5f, 2f, new Color(0.40f, 0.36f, 0.33f, 1f));
                    break;
                case 1: // charred log
                    FillRect(px, size, 2, 6, 12, 4, new Color(0.10f, 0.07f, 0.06f, 1f));
                    FillRect(px, size, 2, 6, 2, 4, new Color(0.22f, 0.15f, 0.11f, 1f));
                    FillRect(px, size, 6, 8, 1, 1, new Color(1f, 0.4f, 0.1f, 1f));
                    FillRect(px, size, 10, 7, 1, 1, new Color(1f, 0.4f, 0.1f, 1f));
                    break;
                case 2: // fern tuft
                    FillEllipse(px, size, 8f, 3.5f, 5f, 2f, new Color(0.10f, 0.16f, 0.08f, 1f));
                    FillRect(px, size, 5, 4, 1, 6, new Color(0.21f, 0.33f, 0.15f, 1f));
                    FillRect(px, size, 8, 4, 1, 9, new Color(0.27f, 0.41f, 0.18f, 1f));
                    FillRect(px, size, 11, 4, 1, 7, new Color(0.21f, 0.33f, 0.15f, 1f));
                    break;
                case 3: // ash patch
                    FillEllipse(px, size, 8f, 8f, 7.5f, 4.5f, new Color(0.12f, 0.11f, 0.11f, 0.55f));
                    FillEllipse(px, size, 8f, 8f, 4.5f, 2.5f, new Color(0.18f, 0.17f, 0.16f, 0.5f));
                    break;
                case 4: // restored patch: damp, recovering peat
                    FillEllipse(px, size, 8f, 8f, 7.5f, 6.5f, new Color(0.10f, 0.30f, 0.26f, 0.55f));
                    FillRect(px, size, 5, 9, 1, 1, new Color(0.35f, 0.70f, 0.55f, 0.85f));
                    FillRect(px, size, 10, 6, 1, 1, new Color(0.35f, 0.70f, 0.55f, 0.85f));
                    FillRect(px, size, 8, 11, 1, 1, new Color(0.35f, 0.70f, 0.55f, 0.85f));
                    FillRect(px, size, 6, 5, 1, 1, new Color(0.35f, 0.70f, 0.55f, 0.85f));
                    break;
                default: // 5: scorched patch
                    FillEllipse(px, size, 8f, 8f, 7.5f, 6.5f, new Color(0.05f, 0.04f, 0.04f, 0.75f));
                    FillRect(px, size, 6, 8, 1, 1, new Color(0.45f, 0.12f, 0.05f, 0.85f));
                    FillRect(px, size, 10, 6, 1, 1, new Color(0.45f, 0.12f, 0.05f, 0.85f));
                    FillRect(px, size, 8, 10, 1, 1, new Color(0.45f, 0.12f, 0.05f, 0.85f));
                    break;
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(px);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            generated.Add(tex);
            generated.Add(sprite);
            return sprite;
        }

        private static void FillEllipse(Color[] px, int size, float cx, float cy, float rx, float ry, Color color)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        px[(y * size) + x] = color;
                    }
                }
            }
        }

        private static void FillRect(Color[] px, int size, int startX, int startY, int width, int height, Color color)
        {
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        px[(y * size) + x] = color;
                    }
                }
            }
        }
    }
}
