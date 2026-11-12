using System;
using UnityEngine;

namespace HazeBuster.Art
{
    /// <summary>Every picture in the game that can be swapped for your own art.</summary>
    public enum ArtId
    {
        Player,
        Ember,
        BurnedStump,
        Gambut,
        FireBoss,
        DewPickup,
        WaterDroplet,
        FireProjectile,
        FoamBomb,
        FoamPuddle,
        BurningGround
    }

    /// <summary>Which point of your picture sits on the object's position.</summary>
    public enum ArtAnchor
    {
        /// <summary>Same anchor as the placeholder it replaces (usually the centre).</summary>
        KeepCurrent,
        Center,
        /// <summary>Bottom centre: the character's feet sit on the ground point. Best for the slanted view.</summary>
        Feet
    }

    /// <summary>
    /// THE place to drop in your own art.
    ///
    /// One entry (slot) per picture. Drag your sprite into a slot and play: it replaces the
    /// placeholder, whatever its pixel size, because it is scaled to a WORLD HEIGHT. A slot that is
    /// empty keeps its placeholder, so you can replace things one at a time.
    ///
    /// Create it with HazeBuster > Art > Set Up Replaceable Art. It is saved as
    /// Assets/Resources/HazeBusterArt.asset and the game finds it by itself.
    /// </summary>
    [CreateAssetMenu(fileName = "HazeBusterArt", menuName = "HazeBuster/Art Library")]
    public class ArtLibrary : ScriptableObject
    {
        public const string ResourceName = "HazeBusterArt";

        [Serializable]
        public class Slot
        {
            [Tooltip("Which object this slot is for. Do not change it.")]
            public ArtId id;

            [Tooltip("Your picture. Leave empty to keep the placeholder.")]
            public Sprite sprite;

            [Tooltip("Optional animation frames (idle or walk loop). When there are two or more, they are played instead of the single sprite above. All frames are scaled together, so keep their canvas size the same.")]
            public Sprite[] animation;

            [Tooltip("Animation speed.")]
            public float framesPerSecond = 8f;

            [Tooltip("How tall the picture is in the world, in world units. 0 = the same height as the placeholder it replaces, so nothing else in the game needs changing.")]
            public float worldHeight = 0f;

            [Tooltip("Which point of your picture sits on the object's position. Feet = bottom centre.")]
            public ArtAnchor anchor = ArtAnchor.KeepCurrent;

            [Tooltip("Size of the circle collider (the hitbox), in world units. 0 = leave the collider as it is. Only works when the object has a CircleCollider2D.")]
            public float hitboxRadius = 0f;

            [Tooltip("A soft oval shadow under the feet. It helps the slanted view read as standing on the ground.")]
            public bool contactShadow = false;

            [Tooltip("Shadow width in world units. 0 = about as wide as the picture.")]
            public float shadowWidth = 0f;
        }

        public Slot[] slots = new Slot[0];

        [Header("Ground")]
        [Tooltip("Your seamless, tileable ground picture (a painted peatland tile). Empty = a generated placeholder. Use HazeBuster > Art > Prepare Selected Texture As Ground on it once.")]
        public Texture2D groundTexture;

        [Tooltip("How many character heights one repeat of the ground picture covers.")]
        public float groundTileInCharacters = 10f;

        public Color groundTint = Color.white;

        [Tooltip("How much a slow light and dark pattern is laid over the ground to hide the repeat. 0 = none.")]
        [Range(0f, 1f)]
        public float groundVariation = 0.3f;

        [Header("Props and ground marks")]
        [Tooltip("Rocks, logs, ferns, trees... scattered over the world. They stand upright with their bottom edge on the ground. Empty = generated placeholders.")]
        public Sprite[] props;

        [Tooltip("Left where a fire was put out. Lies flat on the ground. Empty = placeholder.")]
        public Sprite restoredMark;

        [Tooltip("Left where a fire burned out. Lies flat on the ground. Empty = placeholder.")]
        public Sprite scorchedMark;

        [Header("Shadows")]
        [Range(0f, 1f)]
        public float shadowOpacity = 0.35f;

        private static ArtLibrary instance;
        private static bool searched;

        /// <summary>The library in Assets/Resources, or null when none has been created.</summary>
        public static ArtLibrary Instance
        {
            get
            {
                if (instance == null && !searched)
                {
                    searched = true;
                    instance = Resources.Load<ArtLibrary>(ResourceName);
                }
                return instance;
            }
        }

        public Slot Find(ArtId id)
        {
            if (slots == null) return null;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].id == id)
                {
                    return slots[i];
                }
            }
            return null;
        }

        // Static fields survive a play session when Domain Reload is switched off, so reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            searched = false;
        }
    }
}
