using UnityEngine;
using HazeBuster.Enemies;
using HazeBuster.Environment;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// Pressurized water projectile shot by the Water Gun.
    /// Extinguishes burning hazards, knocks back enemies, and pierces targets.
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

            // Scale sprite
            transform.localScale = Vector3.one * sizeMultiplier;

            // Set velocity
            rb.linearVelocity = direction.normalized * speed;

            // Rotate in flight direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
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
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
