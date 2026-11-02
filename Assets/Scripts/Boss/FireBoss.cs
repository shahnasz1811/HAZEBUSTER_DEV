using System;
using System.Collections;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Enemies;
using HazeBuster.Environment;
using HazeBuster.Utility;

namespace HazeBuster.Boss
{
    /// <summary>
    /// The Mega Wildfire Nexus: The core Fire Boss that symbolizes the slash-and-burn crisis.
    /// Multi-phase boss encounter with fireball barrages, ember summons, and ground infernos.
    /// Defeating this boss cleanses the peatland and triggers VICTORY!
    /// </summary>
    public class FireBoss : EnemyBase
    {
        public static FireBoss Instance { get; private set; }

        public enum BossPhase
        {
            Phase1_BlazeSurge,
            Phase2_HazeSmother,
            Phase3_GambutInferno
        }

        [Header("Boss Identity")]
        public BossPhase currentPhase = BossPhase.Phase1_BlazeSurge;

        [Header("Boss Attacks")]
        public GameObject fireballPrefab;
        public GameObject emberMinionPrefab;
        public GameObject burningGroundPrefab;
        public float attackInterval = 3.5f;

        public event Action<float, float> OnBossHealthChanged; // (current, max)

        private float attackTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            despawnWhenFar = false; // the boss must never vanish when the player runs far away
        }

        protected override void Start()
        {
            base.Start();
            enemyName = "The Mega Wildfire Nexus";
            maxHealth = 1200f;
            currentHealth = maxHealth;
            moveSpeed = 1.2f;
            contactDamage = 35f;
            dewDropAmount = 25; // Massive reward for defeating boss

            OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        protected override void Update()
        {
            base.Update();

            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
            {
                return;
            }

            // Phase transitions based on HP percentage
            float hpPercent = currentHealth / maxHealth;
            if (hpPercent > 0.66f)
            {
                currentPhase = BossPhase.Phase1_BlazeSurge;
            }
            else if (hpPercent > 0.33f)
            {
                currentPhase = BossPhase.Phase2_HazeSmother;
            }
            else
            {
                currentPhase = BossPhase.Phase3_GambutInferno;
            }

            // Attack cycle
            attackTimer += Time.deltaTime;
            float currentCooldown = currentPhase == BossPhase.Phase3_GambutInferno ? attackInterval * 0.7f : attackInterval;

            if (attackTimer >= currentCooldown)
            {
                attackTimer = 0f;
                PerformRandomAttack();
            }
        }

        protected override void Move()
        {
            if (playerTransform == null) return;

            // Looming, slow approach
            Vector2 dir = ((Vector2)(playerTransform.position - transform.position)).normalized;
            rb.linearVelocity = dir * moveSpeed;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = dir.x < 0;
            }
        }

        public override void TakeWaterDamage(float damageAmount)
        {
            base.TakeWaterDamage(damageAmount);
            OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void PerformRandomAttack()
        {
            int attackRoll = UnityEngine.Random.Range(0, 3);

            switch (attackRoll)
            {
                case 0:
                    StartCoroutine(AttackFireballNova());
                    break;
                case 1:
                    StartCoroutine(AttackFireBreathBarrage());
                    break;
                case 2:
                    if (currentPhase == BossPhase.Phase3_GambutInferno)
                        AttackGambutInferno();
                    else
                        AttackSummonEmbers();
                    break;
            }
        }

        /// <summary>
        /// Attack 1: Emits an expanding concentric ring of fireballs in 360 degrees.
        /// </summary>
        private IEnumerator AttackFireballNova()
        {
            if (fireballPrefab == null) yield break;

            int count = currentPhase == BossPhase.Phase3_GambutInferno ? 16 : 10;
            float step = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * step;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                GameObject fb = PoolManager.Spawn(fireballPrefab, transform.position, Quaternion.identity);
                FireProjectile fp = fb.GetComponent<FireProjectile>();
                if (fp != null)
                {
                    fp.Initialize(dir, 18f, 6.5f);
                }
            }

            yield return null;
        }

        /// <summary>
        /// Attack 2: Rapid stream of fireballs aimed at the player's position.
        /// </summary>
        private IEnumerator AttackFireBreathBarrage()
        {
            if (fireballPrefab == null || playerTransform == null) yield break;

            int burstCount = 5;
            for (int i = 0; i < burstCount; i++)
            {
                if (playerTransform == null) break;
                Vector2 dir = ((Vector2)(playerTransform.position - transform.position)).normalized;

                GameObject fb = PoolManager.Spawn(fireballPrefab, transform.position, Quaternion.identity);
                FireProjectile fp = fb.GetComponent<FireProjectile>();
                if (fp != null)
                {
                    fp.Initialize(dir, 20f, 9f);
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        /// <summary>
        /// Attack 3A: Summons a pack of ember swarmers around the boss.
        /// </summary>
        private void AttackSummonEmbers()
        {
            if (emberMinionPrefab == null) return;

            int spawnCount = 4;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnOffset = (Vector3)UnityEngine.Random.insideUnitCircle * 2f;
                Instantiate(emberMinionPrefab, transform.position + spawnOffset, Quaternion.identity);
            }
        }

        /// <summary>
        /// Attack 3B: Ignites several patches of smoldering peat around the arena floor.
        /// </summary>
        private void AttackGambutInferno()
        {
            if (burningGroundPrefab == null) return;

            int patchCount = 6;
            for (int i = 0; i < patchCount; i++)
            {
                Vector3 randomOffset = (Vector3)UnityEngine.Random.insideUnitCircle * 6f;
                Instantiate(burningGroundPrefab, transform.position + randomOffset, Quaternion.identity);
            }
        }

        protected override void Die()
        {
            Debug.Log("<color=green>[FireBoss] Wildfire core extinguished!</color>");
            base.Die();
            GameManager.Instance?.TriggerVictory();
        }
    }
}
