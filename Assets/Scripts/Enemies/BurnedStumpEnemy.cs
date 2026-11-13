using System.Collections;
using UnityEngine;
using HazeBuster.Environment;

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

        // Starting values for a NEW Burned Stump. Once the prefab exists, the numbers in its Inspector
        // are the ones used. (They used to be forced here on every spawn, so changing them did nothing.)
        public BurnedStumpEnemy()
        {
            enemyName = "Smoldering Burned Stump";
            maxHealth = 80f;
            moveSpeed = 1.4f;
            contactDamage = 25f;
            dewDropAmount = 1;
            dewDropTier = DewPickup.DewTier.Large; // 20 EXP per defeated heavy Burned Stump!
        }

        protected override void Start()
        {
            base.Start();
            FinalizeHealth(); // applies the spawner's health multiplier
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
