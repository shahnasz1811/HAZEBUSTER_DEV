using System.Collections.Generic;
using UnityEngine;
using HazeBuster.Utility;

namespace HazeBuster.Art
{
    /// <summary>
    /// Makes one object's picture replaceable.
    ///
    /// On start it looks up its slot (Id) in the Art Library. If you put a sprite, or animation
    /// frames, in that slot, they replace the placeholder, scaled to the slot's World Height, so
    /// any image size fits and nothing else in the game needs changing. An empty slot leaves the
    /// placeholder alone.
    ///
    /// It can also fit the hitbox to the art and add a soft oval shadow under the feet.
    ///
    /// Added to the prefabs by HazeBuster > Art > Set Up Replaceable Art.
    /// </summary>
    [DefaultExecutionOrder(500)] // after the scripts that switch sprites to the upright shader
    [DisallowMultipleComponent]
    public class ArtSlot : MonoBehaviour
    {
        [Tooltip("Which slot of the Art Library this object uses.")]
        public ArtId id;

        private SpriteRenderer sr;
        private Sprite[] frames;
        private float framesPerSecond = 8f;
        private float timer;
        private int frameIndex;
        private bool shadowBuilt;

        // Pictures already scaled for a given size, so a swarm of enemies shares one sprite.
        private static readonly Dictionary<string, Sprite> fitted = new Dictionary<string, Sprite>();
        private static Sprite shadowSprite;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = GetComponentInChildren<SpriteRenderer>(true);
            }
            if (sr == null) return;

            ApplyArt();
        }

        private void Start()
        {
            BuildShadow();
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2 || sr == null) return;

            timer += Time.deltaTime;
            float step = 1f / Mathf.Max(0.1f, framesPerSecond);
            while (timer >= step)
            {
                timer -= step;
                frameIndex = (frameIndex + 1) % frames.Length;
            }
            sr.sprite = frames[frameIndex];
        }

        // ---------------------------------------------------------------- picture

        private void ApplyArt()
        {
            ArtLibrary library = ArtLibrary.Instance;
            ArtLibrary.Slot slot = library != null ? library.Find(id) : null;
            if (slot == null) return;

            Transform t = sr.transform;
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(t.lossyScale.x));
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(t.lossyScale.y));

            // The first real frame decides the size for the whole animation.
            Sprite first = null;
            if (slot.animation != null)
            {
                for (int i = 0; i < slot.animation.Length; i++)
                {
                    if (slot.animation[i] != null)
                    {
                        first = slot.animation[i];
                        break;
                    }
                }
            }
            if (first == null)
            {
                first = slot.sprite;
            }

            // No art in the slot yet, but a World Height is set: scale the placeholder itself to that
            // height. This is what makes the whole library the one place that decides how big things are.
            if (first == null && slot.worldHeight > 0.0001f)
            {
                first = sr.sprite;
            }

            if (first != null)
            {
                Sprite current = sr.sprite;

                // 0 = same height as the placeholder, so the hitbox and the game's scale still fit.
                float targetHeight = slot.worldHeight;
                if (targetHeight <= 0.0001f)
                {
                    targetHeight = current != null
                        ? current.bounds.size.y * scaleY
                        : (first.rect.height / first.pixelsPerUnit) * scaleY;
                }

                // Pixels per unit that makes the picture exactly targetHeight tall in the world.
                float ppu = (first.rect.height * scaleY) / Mathf.Max(0.0001f, targetHeight);
                Vector2 pivot = PivotFor(slot.anchor, current);

                List<Sprite> list = new List<Sprite>();
                if (slot.animation != null)
                {
                    for (int i = 0; i < slot.animation.Length; i++)
                    {
                        if (slot.animation[i] != null)
                        {
                            list.Add(Fit(slot.animation[i], ppu, pivot));
                        }
                    }
                }

                if (list.Count > 1)
                {
                    frames = list.ToArray();
                    framesPerSecond = Mathf.Max(0.1f, slot.framesPerSecond);
                    sr.sprite = frames[0];
                }
                else
                {
                    sr.sprite = Fit(first, ppu, pivot);
                }
            }

            if (slot.hitboxRadius > 0.0001f)
            {
                CircleCollider2D circle = GetComponent<CircleCollider2D>();
                if (circle != null)
                {
                    circle.radius = slot.hitboxRadius / scaleX;
                }
            }
        }

        private static Vector2 PivotFor(ArtAnchor anchor, Sprite current)
        {
            if (anchor == ArtAnchor.Feet) return new Vector2(0.5f, 0f);
            if (anchor == ArtAnchor.Center) return new Vector2(0.5f, 0.5f);

            if (current != null && current.rect.width > 0f && current.rect.height > 0f)
            {
                return new Vector2(current.pivot.x / current.rect.width, current.pivot.y / current.rect.height);
            }
            return new Vector2(0.5f, 0.5f);
        }

        private static Sprite Fit(Sprite source, float pixelsPerUnit, Vector2 pivot)
        {
            string key = source.GetInstanceID() + "|" + Mathf.RoundToInt(pixelsPerUnit * 100f) + "|" +
                         Mathf.RoundToInt(pivot.x * 100f) + "|" + Mathf.RoundToInt(pivot.y * 100f);

            Sprite result;
            if (fitted.TryGetValue(key, out result) && result != null)
            {
                return result;
            }

            result = Sprite.Create(source.texture, source.rect, pivot, Mathf.Max(0.01f, pixelsPerUnit), 0, SpriteMeshType.FullRect);
            result.name = source.name + " (fitted)";
            fitted[key] = result;
            return result;
        }

        // ---------------------------------------------------------------- contact shadow

        private void BuildShadow()
        {
            if (shadowBuilt || sr == null || sr.sprite == null) return;
            shadowBuilt = true;

            ArtLibrary library = ArtLibrary.Instance;
            ArtLibrary.Slot slot = library != null ? library.Find(id) : null;
            if (slot == null || !slot.contactShadow) return;

            // In the flat 2D view there is no slant, so a shadow would only look odd.
            if (!SlantedPerspective.IsSlanted) return;

            Transform parent = sr.transform;
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(parent.lossyScale.x));
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(parent.lossyScale.y));
            Bounds bounds = sr.sprite.bounds;

            float worldWidth = slot.shadowWidth > 0.0001f ? slot.shadowWidth : bounds.size.x * scaleX * 0.9f;

            // The sprite stands upright, but the shadow lies on the ground. A distance d below the
            // pivot on screen is d / cos(tilt) along the ground, because the ground is foreshortened.
            float cosTilt = Mathf.Max(0.25f, Mathf.Cos(SlantedPerspective.TiltDegrees * Mathf.Deg2Rad));
            float feetOffset = bounds.min.y / cosTilt;

            GameObject go = new GameObject("Contact Shadow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, feetOffset, 0f);
            go.transform.localScale = new Vector3(worldWidth / scaleX, worldWidth / scaleY, 1f);

            SpriteRenderer shadowRenderer = go.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GetShadowSprite();
            shadowRenderer.color = new Color(0f, 0f, 0f, library.shadowOpacity);
            shadowRenderer.sortingLayerID = sr.sortingLayerID;
            shadowRenderer.sortingOrder = sr.sortingOrder - 1;
        }

        // A soft oval, one world unit wide, shared by every shadow.
        private static Sprite GetShadowSprite()
        {
            if (shadowSprite != null) return shadowSprite;

            const int width = 64;
            const int height = 32;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.name = "Contact Shadow (generated)";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;

            Color[] px = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f - (width * 0.5f)) / (width * 0.5f);
                    float dy = (y + 0.5f - (height * 0.5f)) / (height * 0.5f);
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float a = Mathf.Clamp01(1f - distance);
                    a = a * a * (3f - (2f * a)); // soft edge
                    px[(y * width) + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            shadowSprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
            shadowSprite.name = "Contact Shadow (generated)";
            shadowSprite.hideFlags = HideFlags.HideAndDontSave;
            return shadowSprite;
        }

        // Static fields survive a play session when Domain Reload is switched off, so reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            fitted.Clear();
            shadowSprite = null;
        }
    }
}
