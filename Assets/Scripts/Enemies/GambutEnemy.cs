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
        public float trailDropInterval = 3.5f;
        private float trailTimer = 0f;

        // Starting values for a NEW Gambut. Once the prefab exists, the numbers in its Inspector are
        // the ones used. (They used to be forced here on every spawn, so changing them did nothing.)
        public GambutEnemy()
        {
            enemyName = "Gambut Creeper";
            maxHealth = 45f;
            moveSpeed = 2.0f;
            contactDamage = 18f;
            dewDropAmount = 1;
            dewDropTier = DewPickup.DewTier.Medium; // 5 EXP per defeated Gambut monster!
        }

        protected override void Start()
        {
            base.Start();
            FinalizeHealth(); // applies the spawner's health multiplier
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
