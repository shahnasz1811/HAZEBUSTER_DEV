using UnityEngine;
using HazeBuster.Environment;

namespace HazeBuster.Enemies
{
    /// <summary>
    /// Gambut Monster (Api Bawah Tanah).
    /// Subterranean peat hazard that creeps through organic soil layers.
    /// Leaves smoldering burning ground in its wake.
    /// </summary>
    public class GambutEnemy : EnemyBase
    {
        [Header("Gambut Ground Hazard Settings")]
        public GameObject burningGroundPrefab;
        public float trailDropInterval = 1.8f;
        private float trailTimer = 0f;

        protected override void Start()
        {
            base.Start();
            enemyName = "Gambut Creeper";
            maxHealth = 45f;
            FinalizeHealth(); // applies the spawner's health multiplier
            moveSpeed = 2.0f;
            contactDamage = 18f;
            dewDropAmount = 1;
            dewDropTier = DewPickup.DewTier.Medium; // 5 EXP per defeated Gambut monster!
        }

        protected override void Update()
        {
            base.Update();

            trailTimer += Time.deltaTime;
            if (trailTimer >= trailDropInterval)
            {
                trailTimer = 0f;
                DropBurningGround();
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

        private void DropBurningGround()
        {
            if (burningGroundPrefab != null)
            {
                Instantiate(burningGroundPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
