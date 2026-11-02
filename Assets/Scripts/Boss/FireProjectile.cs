using UnityEngine;
using HazeBuster.Player;
using HazeBuster.Utility;
using HazeBuster.Weapons;

namespace HazeBuster.Boss
{
    /// <summary>
    /// Fireball launched by the Fire Boss.
    /// Damages the player on contact, but can be extinguished mid-air by water projectiles!
    /// Fireballs are pooled: Initialize() resets everything for a new flight.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class FireProjectile : MonoBehaviour
    {
        public float damage = 20f;
        public float speed = 7f;
        public float lifetime = 6f;

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

        public void Initialize(Vector2 direction, float projDamage, float projSpeed)
        {
            damage = projDamage;
            speed = projSpeed;

            // A pooled fireball is reused, so restart its lifetime.
            lifeTimer = lifetime;
            inFlight = true;

            rb.linearVelocity = direction.normalized * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            if (!inFlight) return;

            // Uses scaled time like the old Destroy(gameObject, lifetime), so it pauses with the game.
            lifeTimer -= Time.deltaTime;
            // A fireball also ends at the arena edge (2 unit grace) so it never flies through empty space.
            if (lifeTimer <= 0f || !ArenaBounds.Contains(transform.position, -2f))
            {
                Release();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Ignore hits that arrive after this fireball has already been returned to the pool.
            if (!inFlight) return;

            if (collision.CompareTag("Player"))
            {
                PlayerHealth health = collision.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    Release();
                }
            }
            // If water hits the fireball, it extinguishes with steam!
            else if (collision.GetComponent<WaterProjectile>() != null)
            {
                Release();
            }
        }

        /// <summary>
        /// Ends this flight and returns the fireball to the pool (or destroys it if it was not pooled).
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
