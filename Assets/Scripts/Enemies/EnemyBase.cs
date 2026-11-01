using System.Collections;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Player;

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

        [Header("Drop Prefabs")]
        public GameObject dewPrefab;

        [Header("Visuals & Feedback")]
        public SpriteRenderer spriteRenderer;
        public Color hurtColor = Color.cyan; // Flashes water blue when hit
        protected Color originalColor = Color.white;

        protected Transform playerTransform;
        protected Rigidbody2D rb;
        protected bool isKnockedBack = false;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
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
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
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
        }

        protected abstract void Move();

        public virtual void TakeWaterDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            StartCoroutine(FlashHurtRoutine());

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (!gameObject.activeInHierarchy) return;
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
            // Spawn reward dew drops
            if (dewPrefab != null)
            {
                for (int i = 0; i < dewDropAmount; i++)
                {
                    Vector3 offset = (Vector3)Random.insideUnitCircle * 0.4f;
                    Instantiate(dewPrefab, transform.position + offset, Quaternion.identity);
                }
            }

            GameManager.Instance?.RegisterEnemyDefeated();
            Destroy(gameObject);
        }
    }
}
