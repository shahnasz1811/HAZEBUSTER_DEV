using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Makes the peatland ground completely endless with zero jitter!
    /// Only recenters the ground in large, exact multi-tile chunks (16 units)
    /// when the player travels far, keeping the ground 100% stationary for normal movement.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InfiniteGround : MonoBehaviour
    {
        [Header("Target Tracking")]
        public Transform target;

        [Header("Tile Settings")]
        public float tileSize = 1f;
        public int tilesPerChunk = 16; // Shift only when player moves 16 full tiles away

        [Header("Arena")]
        [Tooltip("When ArenaBounds is on, the ground sits still on the arena and covers it exactly instead of following the player.")]
        public bool fitToArena = true;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            // Auto-detect tile size from sprite PPU
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                tileSize = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
            }

            // With a bounded arena the ground covers the arena exactly and never moves.
            if (fitToArena && ArenaBounds.enabled && spriteRenderer != null)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Tiled;
                float side = Mathf.Max(70f, ArenaBounds.halfSize * 2f + 4f);
                spriteRenderer.size = new Vector2(side, side);
                transform.position = new Vector3(ArenaBounds.center.x, ArenaBounds.center.y, transform.position.z);
            }

            // Ensure Draw Mode is Tiled and wide enough to cover camera view
            if (spriteRenderer != null)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Tiled;
                if (spriteRenderer.size.x < 70f || spriteRenderer.size.y < 70f)
                {
                    spriteRenderer.size = new Vector2(70f, 70f);
                }
            }
        }

        private void LateUpdate()
        {
            // Bounded arena: the ground is fixed on the arena, so there is nothing to follow.
            if (fitToArena && ArenaBounds.enabled) return;
            FollowTarget();
        }

        private void FollowTarget()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
                return;
            }

            float chunkStep = tileSize * tilesPerChunk;
            float dx = target.position.x - transform.position.x;
            float dy = target.position.y - transform.position.y;

            // Only shift the ground when the player moves beyond an entire chunk!
            if (Mathf.Abs(dx) >= chunkStep)
            {
                float shiftX = Mathf.Round(dx / chunkStep) * chunkStep;
                transform.position = new Vector3(transform.position.x + shiftX, transform.position.y, transform.position.z);
            }

            if (Mathf.Abs(dy) >= chunkStep)
            {
                float shiftY = Mathf.Round(dy / chunkStep) * chunkStep;
                transform.position = new Vector3(transform.position.x, transform.position.y + shiftY, transform.position.z);
            }
        }
    }
}
