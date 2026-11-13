using System.Collections;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Player;
using HazeBuster.Environment;
using HazeBuster.Utility;

namespace HazeBuster.Enemies
{
    /// <summary>
    /// Abstract base class for all fire hazards and smog creatures in HAZEBUSTER.
    /// Handles health, movement toward player, taking water damage, knockback, and dew drops.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Base Stats")]
        public string enemyName = "Fire Hazard";
        public float maxHealth = 30f;
        public float currentHealth;
        public float moveSpeed = 3f;
        public float contactDamage = 15f;
        public int dewDropAmount = 1;

        [Header("Difficulty")]
        [Tooltip("Set by the spawner right after the enemy is created. 1 = normal health, 1.5 = +50%.")]
        public float healthMultiplier = 1f;

        [Header("Drop Prefabs & EXP Tier")]
        public GameObject dewPrefab;
        public DewPickup.DewTier dewDropTier = DewPickup.DewTier.Small;

        [Header("Visuals & Feedback")]
        public SpriteRenderer spriteRenderer;
        public Color hurtColor = Color.cyan; // Flashes water blue when hit
        protected Color originalColor = Color.white;

        [Header("Despawn")]
        [Tooltip("Enemies far behind the player are removed to save performance. The boss turns this off.")]
        public bool despawnWhenFar = true;
        public float despawnDistance = 35f;

        protected Transform playerTransform;
        protected Rigidbody2D rb;
        protected bool isKnockedBack = false;
        protected bool isDead = false;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            // Slanted camera: draw the enemy upright so it is not squashed with the ground.
            SlantedPerspective.MakeUpright(gameObject);
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        protected virtual void Start()
        {
            currentHealth = maxHealth;
            playerTransform = PlayerLocator.PlayerTransform;
        }

        /// <summary>
        /// Call once at the end of Start(), after the enemy's own base stats are set.
        /// Applies the spawner's health multiplier to the Max Health from the Inspector and fills health.
        /// </summary>
        protected void FinalizeHealth()
        {
            maxHealth *= healthMultiplier;
            currentHealth = maxHealth;
        }

        protected virtual void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (!isKnockedBack)
            {
                Move();
            }

            // Endless map optimization: despawn if player outruns enemy into deep haze
            if (despawnWhenFar && playerTransform != null)
            {
                Vector2 offset = transform.position - playerTransform.position;
                if (offset.sqrMagnitude > despawnDistance * despawnDistance)
                {
                    Destroy(gameObject);
                }
            }
        }

        protected abstract void Move();

        public virtual void TakeWaterDamage(float damageAmount)
        {
            // Several shots can land in the same frame; once dead, ignore the rest.
            if (isDead) return;

            currentHealth -= damageAmount;
            StartCoroutine(FlashHurtRoutine());

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (isDead || !gameObject.activeInHierarchy) return;
            StartCoroutine(KnockbackRoutine(direction, force));
        }

        protected IEnumerator KnockbackRoutine(Vector2 direction, float force)
        {
            isKnockedBack = true;
            rb.linearVelocity = direction * force;
            yield return new WaitForSeconds(0.15f);
            rb.linearVelocity = Vector2.zero;
            isKnockedBack = false;
        }

        protected IEnumerator FlashHurtRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtColor;
                yield return new WaitForSeconds(0.08f);
                spriteRenderer.color = originalColor;
            }
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            if (isDead) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(contactDamage);
                }
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D collision)
        {
            if (isDead) return;

            if (collision.CompareTag("Player"))
            {
                PlayerHealth health = collision.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(contactDamage);
                }
            }
        }

        protected virtual void Die()
        {
            // Guard against two shots killing the same enemy in one frame (double drops).
            if (isDead) return;
            isDead = true;

            // Spawn reward dew drops with assigned EXP tier
            if (dewPrefab != null)
            {
                for (int i = 0; i < dewDropAmount; i++)
                {
                    Vector3 offset = (Vector3)Random.insideUnitCircle * 0.4f;
                    GameObject drop = Instantiate(dewPrefab, transform.position + offset, Quaternion.identity);
                    DewPickup pickup = drop.GetComponent<DewPickup>();
                    if (pickup != null)
                    {
                        pickup.SetTier(dewDropTier);
                    }
                }
            }

            GameManager.Instance?.RegisterEnemyDefeated();
            Destroy(gameObject);
        }
    }
}
