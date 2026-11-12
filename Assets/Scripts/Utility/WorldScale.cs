using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// The size of the world, measured by the player: how tall the player character looks, in
    /// world units.
    ///
    /// The ground texture, the props and the ground marks are all sized in multiples of this, so
    /// they stay in proportion with the characters whatever size your art has. Zoom the camera
    /// (Orthographic Size) to decide how big everything looks on screen.
    /// </summary>
    public static class WorldScale
    {
        private static float characterHeight;

        /// <summary>True once the player has been found and measured.</summary>
        public static bool IsReady
        {
            get { return Measure() > 0f; }
        }

        /// <summary>The player's height in world units, or 1 while no player has been found.</summary>
        public static float CharacterHeight
        {
            get
            {
                float h = Measure();
                return h > 0f ? h : 1f;
            }
        }

        private static float Measure()
        {
            if (characterHeight > 0f) return characterHeight;

            Transform player = PlayerLocator.PlayerTransform;
            if (player == null) return 0f;

            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr == null) sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return 0f;

            float h = sr.sprite.bounds.size.y * Mathf.Abs(sr.transform.lossyScale.y);
            if (h > 0.001f)
            {
                characterHeight = h;
            }
            return characterHeight;
        }

        // Static fields survive a play session when Domain Reload is switched off, so reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            characterHeight = 0f;
        }
    }
}
