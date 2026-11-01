using UnityEngine;
using HazeBuster.Player;
using HazeBuster.Weapons;

namespace HazeBuster.Boss
{
    /// <summary>
    /// Fireball launched by the Fire Boss.
    /// Damages the player on contact, but can be extinguished mid-air by water projectiles!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class FireProjectile : MonoBehaviour
    {
        public float damage = 20f;
        public float speed = 7f;
        public float lifetime = 6f;

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

            rb.linearVelocity = direction.normalized * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                PlayerHealth health = collision.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    Destroy(gameObject);
                }
            }
            // If water hits the fireball, it extinguishes with steam!
            else if (collision.GetComponent<WaterProjectile>() != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
