using System.Collections;
using UnityEngine;

namespace HazeBuster.Enemies
{
    /// <summary>
    /// Smoldering Burned Stump (Tunggul Bakar).
    /// Heavy tank enemy left from illegal logging.
    /// Retains subterranean heat, moves slowly, and periodically spews blinding smoke.
    /// </summary>
    public class BurnedStumpEnemy : EnemyBase
    {
        [Header("Stump Smoke Puff")]
        public float smokePuffInterval = 6f;
        public float smokePuffRadius = 3f;
        public GameObject smokeParticlePrefab;

        private float smokeTimer = 0f;

        protected override void Start()
        {
            base.Start();
            enemyName = "Smoldering Burned Stump";
            maxHealth = 80f;
            currentHealth = maxHealth;
            moveSpeed = 1.4f;
            contactDamage = 25f;
            dewDropAmount = 3;
        }

        protected override void Update()
        {
            base.Update();

            smokeTimer += Time.deltaTime;
            if (smokeTimer >= smokePuffInterval)
            {
                smokeTimer = 0f;
                EmitSmokePuff();
            }
        }

        protected override void Move()
        {
            if (playerTransform == null) return;

            Vector2 dir = ((Vector2)(playerTransform.position - transform.position)).normalized;
            rb.linearVelocity = dir * moveSpeed;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = dir.x < 0;
            }
        }

        private void EmitSmokePuff()
        {
            if (smokeParticlePrefab != null)
            {
                Instantiate(smokeParticlePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                // Visual feedback: pulse sprite briefly
                StartCoroutine(PulseSmokeVisual());
            }
        }

        private IEnumerator PulseSmokeVisual()
        {
            if (spriteRenderer != null)
            {
                Color darkSmoke = new Color(0.2f, 0.2f, 0.2f, 1f);
                spriteRenderer.color = darkSmoke;
                yield return new WaitForSeconds(0.4f);
                spriteRenderer.color = originalColor;
            }
        }
    }
}
