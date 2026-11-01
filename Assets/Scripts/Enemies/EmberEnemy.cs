using UnityEngine;

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

        protected override void Start()
        {
            base.Start();
            enemyName = "Airborne Ember";
            maxHealth = 15f;
            currentHealth = maxHealth;
            moveSpeed = 4.2f;
            contactDamage = 10f;
            dewDropAmount = 1;

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
