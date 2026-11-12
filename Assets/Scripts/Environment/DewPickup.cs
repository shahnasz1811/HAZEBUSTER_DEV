using System.Collections;
using UnityEngine;
using HazeBuster.Player;
using HazeBuster.Utility;

namespace HazeBuster.Environment
{
    /// <summary>
    /// Morning Dew / Moisture droplet collectible.
    /// Dropped by extinguished fire hazards.
    /// Stays resting on the peat ground until the player walks within pickup range.
    /// Features 3 distinct tiers (Small, Medium, Large) for balanced EXP scaling over time!
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DewPickup : MonoBehaviour
    {
        public enum DewTier
        {
            Small = 1,      // 1 EXP (Common embers)
            Medium = 5,     // 5 EXP (Mid-game / Gambut)
            Large = 20      // 20 EXP (Heavy stumps & late-game)
        }

        [Header("Dew Tier & Value")]
        public DewTier tier = DewTier.Small;
        public int expValue = 1;

        [Header("Magnet Movement")]
        public float flySpeed = 7f;
        public float acceleration = 18f;

        [Header("Visual Feedback")]
        public SpriteRenderer spriteRenderer;

        private Transform playerTransform;
        private PlayerExperience playerExp;
        private bool isBeingAttracted = false;
        private bool collected = false; // stops the pickup being counted twice in one frame
        private float currentSpeed;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;

            // Slanted camera: draw the pickup upright so it is not squashed with the ground.
            SlantedPerspective.MakeUpright(gameObject);
        }

        private void Start()
        {
            playerTransform = PlayerLocator.PlayerTransform;
            playerExp = PlayerLocator.Experience;

            currentSpeed = flySpeed;
            ApplyTierVisuals();

            // Gentle initial pop effect when dropped
            StartCoroutine(PopAnimation());
        }

        public void SetTier(DewTier newTier)
        {
            tier = newTier;
            expValue = (int)newTier;
            ApplyTierVisuals();
        }

        private void ApplyTierVisuals()
        {
            if (spriteRenderer == null) return;

            switch (tier)
            {
                case DewTier.Small:
                    transform.localScale = Vector3.one * 0.9f;
                    spriteRenderer.color = new Color(0.4f, 0.9f, 1f, 1f); // Cool cyan
                    break;
                case DewTier.Medium:
                    transform.localScale = Vector3.one * 1.3f;
                    spriteRenderer.color = new Color(0.3f, 1f, 0.6f, 1f); // Vibrant emerald
                    break;
                case DewTier.Large:
                    transform.localScale = Vector3.one * 1.8f;
                    spriteRenderer.color = new Color(1f, 0.85f, 0.2f, 1f); // Golden pure moisture
                    break;
            }
        }

        private IEnumerator PopAnimation()
        {
            // Little pop outward so it spreads naturally from the defeated enemy
            Vector3 popDir = (Vector3)Random.insideUnitCircle * 0.6f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + popDir;

            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Arc hop
                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                current.y += Mathf.Sin(t * Mathf.PI) * 0.3f;
                transform.position = current;
                yield return null;
            }
            transform.position = targetPos;
        }

        private void Update()
        {
            if (playerTransform == null) return;

            float distance = Vector2.Distance(transform.position, playerTransform.position);

            // Controlled strictly by player's upgraded pickup/magnet range
            float pickupRange = playerExp != null ? playerExp.currentMagnetRadius : 1.2f;

            if (distance <= pickupRange)
            {
                isBeingAttracted = true;
            }

            if (isBeingAttracted)
            {
                currentSpeed += acceleration * Time.deltaTime;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTransform.position,
                    currentSpeed * Time.deltaTime
                );

                // Collect when reaching player
                if (distance < 0.45f)
                {
                    Collect();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Collect();
            }
        }

        private void Collect()
        {
            // Update() and OnTriggerEnter2D can both reach this in the same frame.
            if (collected) return;
            collected = true;

            if (playerExp != null)
            {
                playerExp.CollectDew(expValue);
            }
            Destroy(gameObject);
        }
    }
}
