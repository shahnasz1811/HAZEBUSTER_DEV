using UnityEngine;
using HazeBuster.Environment;

namespace HazeBuster.Enemies
{
    /// <summary>
    /// Small airborne ember (Bara Api).
    /// Fast swarmer that zig-zags erratically on the wind.
    /// Low health, high speed.
    /// </summary>
    public class EmberEnemy : EnemyBase
    {
        [Header("Ember Movement")]
        public float wobbleFrequency = 5f;
        public float wobbleAmplitude = 1.2f;

        private float wobbleSeed;

        // Starting values for a NEW Ember. Once the prefab exists, the numbers in its Inspector are the
        // ones used. (They used to be forced here on every spawn, so changing them did nothing.)
        public EmberEnemy()
        {
            enemyName = "Airborne Ember";
            maxHealth = 15f;
            moveSpeed = 3f;
            contactDamage = 10f;
            dewDropAmount = 1;
        }

        protected override void Start()
        {
            base.Start();
            FinalizeHealth(); // applies the spawner's health multiplier

            wobbleSeed = Random.Range(0f, 100f);
        }

        protected override void Move()
        {
            if (playerTransform == null) return;

            Vector2 directToPlayer = ((Vector2)(playerTransform.position - transform.position)).normalized;
            Vector2 perpendicular = new Vector2(-directToPlayer.y, directToPlayer.x);

            float wobble = Mathf.Sin((Time.time + wobbleSeed) * wobbleFrequency) * wobbleAmplitude;
            Vector2 finalMove = (directToPlayer + (perpendicular * wobble)).normalized;

            rb.linearVelocity = finalMove * moveSpeed;

            // Orient towards player
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = directToPlayer.x < 0;
            }
        }
    }
}
