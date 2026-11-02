using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Optional square play area (60 x 60 by default).
    ///
    /// The world is ENDLESS by default, so `enabled` is false and every helper here lets everything
    /// through: the player, enemies, projectiles and the camera are not limited at all.
    /// Set `ArenaBounds.enabled = true` only for a closed fight, for example a boss arena.
    /// While it is on, the player is clamped with Clamp(), shots end at the edge, and the camera
    /// keeps its view inside the square.
    /// </summary>
    public static class ArenaBounds
    {
        public static Vector2 center = Vector2.zero;
        public static float halfSize = 30f;   // 60 x 60 when the arena is switched on
        public static bool enabled = false;   // endless world by default

        public static Vector2 Clamp(Vector2 p, float margin = 0.5f)
        {
            if (!enabled) return p;
            float h = Mathf.Max(0.5f, halfSize - margin);
            p.x = Mathf.Clamp(p.x, center.x - h, center.x + h);
            p.y = Mathf.Clamp(p.y, center.y - h, center.y + h);
            return p;
        }

        public static bool Contains(Vector2 p, float margin = 0f)
        {
            if (!enabled) return true;
            float h = halfSize - margin;
            return Mathf.Abs(p.x - center.x) <= h && Mathf.Abs(p.y - center.y) <= h;
        }

        public static Vector2 RandomPoint(float margin = 2f)
        {
            float h = Mathf.Max(0.5f, halfSize - margin);
            return new Vector2(center.x + Random.Range(-h, h), center.y + Random.Range(-h, h));
        }
    }
}
