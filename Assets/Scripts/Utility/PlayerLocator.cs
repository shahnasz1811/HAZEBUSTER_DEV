using UnityEngine;
using HazeBuster.Player;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Finds the Player once and remembers it, so enemies, pickups and managers do not each
    /// call FindGameObjectWithTag (slow when dozens of enemies start at the same time).
    /// The cache refreshes itself after a scene reload or if the player object is destroyed.
    /// </summary>
    public static class PlayerLocator
    {
        private static Transform cachedTransform;
        private static PlayerExperience cachedExperience;

        /// <summary>The Player's transform, or null if no object tagged "Player" exists.</summary>
        public static Transform PlayerTransform
        {
            get
            {
                if (cachedTransform == null)
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    cachedTransform = player != null ? player.transform : null;
                    cachedExperience = player != null ? player.GetComponent<PlayerExperience>() : null;
                }
                return cachedTransform;
            }
        }

        /// <summary>The Player's PlayerExperience component, or null if it cannot be found.</summary>
        public static PlayerExperience Experience
        {
            get
            {
                Transform playerTransform = PlayerTransform;
                if (playerTransform != null && cachedExperience == null)
                {
                    cachedExperience = playerTransform.GetComponent<PlayerExperience>();
                }
                return cachedExperience;
            }
        }
    }
}
