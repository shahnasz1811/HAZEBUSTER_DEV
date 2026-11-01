using System.Collections;
using UnityEngine;
using HazeBuster.Environment;
using HazeBuster.Enemies;

namespace HazeBuster.Player
{
    /// <summary>
    /// Dropped along the player's path during a Hydro-Dash.
    /// Douses burning ground immediately and damages/slows passing fire enemies.
    /// </summary>
    public class WaterTrail : MonoBehaviour
    {
        [Header("Settings")]
        public float lifetime = 2.5f;
        public float damagePerSecond = 20f;
        public float slowFactor = 0.5f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                // Subtle translucent water blue
                spriteRenderer.color = new Color(0.2f, 0.7f, 1f, 0.6f);
            }

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.6f;
            }
        }

        private void Start()
        {
            StartCoroutine(FadeAndDestroyRoutine());
        }

        private IEnumerator FadeAndDestroyRoutine()
        {
            float elapsed = 0f;
            Color startCol = spriteRenderer.color;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startCol.a, 0f, elapsed / lifetime);
                spriteRenderer.color = new Color(startCol.r, startCol.g, startCol.b, alpha);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Extinguish burning ground
            if (other.CompareTag("Hazard") || other.GetComponent<BurningGround>() != null)
            {
                BurningGround bg = other.GetComponent<BurningGround>();
                if (bg != null)
                {
                    bg.Extinguish();
                }
            }

            // Damage enemy
            if (other.CompareTag("Enemy"))
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeWaterDamage(damagePerSecond * 0.5f);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeWaterDamage(damagePerSecond * Time.deltaTime);
                }
            }
        }
    }
}
