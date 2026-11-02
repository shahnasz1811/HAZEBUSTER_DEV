using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// The playable peatland arena. The player, enemies, hazards and decor all share this
    /// one square, so the world stays bounded and the boss fight has a real arena.
    /// No physics walls are needed: everything is clamped with ArenaBounds.Clamp().
    /// </summary>
    public static class ArenaBounds
    {
        public static Vector2 center = Vector2.zero;
        public static float halfSize = 30f;   // 60 x 60 world units (matches HazeBuster > Build Arena Walls)
        public static bool enabled = true;

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
