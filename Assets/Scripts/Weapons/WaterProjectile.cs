using UnityEngine;
using HazeBuster.Enemies;
using HazeBuster.Environment;
using HazeBuster.Utility;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// Pressurized water projectile shot by the Water Gun.
    /// Extinguishes burning hazards, knocks back enemies, and pierces targets.
    /// Projectiles are pooled: Initialize() resets everything for a new flight.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class WaterProjectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        public float speed = 14f;
        public float damage = 15f;
        public float knockbackForce = 6f;
        public int pierceCount = 1;
        public float lifetime = 3f;

        private int currentPierces = 0;
        private float lifeTimer = 0f;
        private bool inFlight = false;
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Initialize(Vector2 direction, float projDamage, float projSpeed, float sizeMultiplier, int maxPierce, float knockback)
        {
            damage = projDamage;
            speed = projSpeed;
            pierceCount = maxPierce;
            knockbackForce = knockback;

            // A pooled projectile is reused, so reset everything left over from its last flight.
            currentPierces = 0;
            lifeTimer = lifetime;
            inFlight = true;

            // Scale sprite
            transform.localScale = Vector3.one * sizeMultiplier;

            // Set velocity
            rb.linearVelocity = direction.normalized * speed;

            // Rotate in flight direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            if (!inFlight) return;

            // Uses scaled time like the old Destroy(gameObject, lifetime), so it pauses with the game.
            lifeTimer -= Time.deltaTime;
            // A shot also ends at the arena edge (2 unit grace) so it never flies through empty space.
            if (lifeTimer <= 0f || !ArenaBounds.Contains(transform.position, -2f))
            {
                Release();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Ignore hits that arrive after this projectile has already been returned to the pool.
            if (!inFlight) return;

            // 1. Extinguish burning peat ground hazards
            if (collision.CompareTag("Hazard") || collision.GetComponent<BurningGround>() != null)
            {
                BurningGround bg = collision.GetComponent<BurningGround>();
                if (bg != null)
                {
                    bg.Extinguish();
                }
            }

            // 2. Hit burning enemies
            if (collision.CompareTag("Enemy"))
            {
                EnemyBase enemy = collision.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeWaterDamage(damage);
                    Vector2 hitDir = rb.linearVelocity.normalized;
                    enemy.ApplyKnockback(hitDir, knockbackForce);

                    currentPierces++;
                    if (currentPierces >= pierceCount)
                    {
                        Release();
                    }
                }
            }
        }

        /// <summary>
        /// Ends this flight and returns the projectile to the pool (or destroys it if it was not pooled).
        /// </summary>
        private void Release()
        {
            if (!inFlight) return;
            inFlight = false;

            rb.linearVelocity = Vector2.zero;
            PoolManager.Despawn(gameObject);
        }
    }
}
