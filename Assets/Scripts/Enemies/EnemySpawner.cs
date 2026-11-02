using System.Collections.Generic;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Player;
using HazeBuster.Boss;
using HazeBuster.Utility;

namespace HazeBuster.Enemies
{
    /// <summary>
    /// Spawns fire hazards just outside the player's vision edge so they emerge from the haze.
    /// Manages escalating survival waves, difficulty scaling over time, and boss awakening.
    /// Spawn points are kept inside ArenaBounds only while that arena is switched on (the world is endless by default).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Prefabs")]
        public GameObject emberPrefab;
        public GameObject burnedStumpPrefab;
        public GameObject gambutPrefab;
        public GameObject fireBossPrefab;

        [Header("Spawn Settings")]
        public float spawnRadius = 12f; // Outside typical vision radius
        public float baseSpawnInterval = 1.8f; // Interval at 00:00
        public float minSpawnInterval = 0.35f; // Interval at climax (fast swarms)
        public int maxEnemiesAlive = 65;

        [Header("Runtime Difficulty")]
        [Tooltip("Multiplies enemy HP as time passes (e.g. +10% every minute)")]
        public float hpScalePerMinute = 0.12f;

        private Transform playerTransform;
        private PlayerVision playerVision;
        private float spawnTimer = 0f;
        private List<GameObject> activeEnemies = new List<GameObject>();

        private void Start()
        {
            playerTransform = PlayerLocator.PlayerTransform;
            if (playerTransform != null)
            {
                playerVision = playerTransform.GetComponent<PlayerVision>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            {
                return;
            }

            // Clean up null references from defeated/despawned enemies
            activeEnemies.RemoveAll(e => e == null);

            // Calculate difficulty ramp based on elapsed run time
            float progress = Mathf.Clamp01(GameManager.Instance.currentRunTimer / GameManager.Instance.runDurationToBoss);
            float currentSpawnInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress);

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentSpawnInterval)
            {
                spawnTimer = 0f;
                if (activeEnemies.Count < maxEnemiesAlive)
                {
                    SpawnNextEnemy(progress);
                }
            }
        }

        private void SpawnNextEnemy(float runProgress)
        {
            if (playerTransform == null) return;

            // Determine spawn position just outside the player's vision circle
            float currentVision = playerVision != null ? playerVision.currentVisionRadius : 6f;
            float spawnDist = Mathf.Max(spawnRadius, currentVision + 3.0f);

            Vector2 spawnPos = FindSpawnPosition(playerTransform.position, spawnDist);

            // Pick enemy type based on run progress (Wave timeline)
            GameObject prefabToSpawn = emberPrefab;

            float roll = Random.value;
            if (runProgress > 0.65f) // Late run: Gambut & Stumps frequent
            {
                if (roll < 0.35f) prefabToSpawn = emberPrefab;
                else if (roll < 0.70f) prefabToSpawn = gambutPrefab;
                else prefabToSpawn = burnedStumpPrefab;
            }
            else if (runProgress > 0.30f) // Mid run: Burned Stumps introduced
            {
                if (roll < 0.60f) prefabToSpawn = emberPrefab;
                else prefabToSpawn = burnedStumpPrefab;
            }
            else // Early run: fast airborne embers
            {
                prefabToSpawn = emberPrefab;
            }

            // If the chosen prefab was not assigned in the Inspector, fall back to an ember
            // so the wave never goes silent.
            if (prefabToSpawn == null)
            {
                prefabToSpawn = emberPrefab;
            }

            if (prefabToSpawn != null)
            {
                GameObject enemyObj = Instantiate(prefabToSpawn, (Vector3)spawnPos, Quaternion.identity);
                EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();

                if (enemy != null && GameManager.Instance != null)
                {
                    // Scale HP over time (minutes elapsed). The enemy applies this multiplier in its
                    // own Start(), after it has set its base stats.
                    float minutesElapsed = GameManager.Instance.currentRunTimer / 60f;
                    enemy.healthMultiplier = 1f + (minutesElapsed * hpScalePerMinute);
                }

                activeEnemies.Add(enemyObj);
            }
        }

        /// <summary>
        /// Picks a point on a ring around the player that lies inside the arena.
        /// Tries a few random angles first; if the player is cornered and all of them fall
        /// outside the arena, the last point is clamped inside instead.
        /// </summary>
        private static Vector2 FindSpawnPosition(Vector2 center, float distance)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (ArenaBounds.Contains(candidate, 1f))
                {
                    return candidate;
                }
            }

            float fallbackAngle = Random.value * Mathf.PI * 2f;
            Vector2 fallback = center + new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle)) * distance;
            return ArenaBounds.Clamp(fallback, 1f);
        }

        public void SpawnFireBoss()
        {
            if (fireBossPrefab == null)
            {
                Debug.LogWarning("[HAZEBUSTER] No Fire Boss prefab is assigned on the EnemySpawner, so the boss cannot appear and the run cannot be won.");
                return;
            }
            if (playerTransform == null) return;

            // With a closed arena, spawn the boss on the side with more room. In the endless world any direction is fine.
            Vector2 awayFromEdge = ArenaBounds.enabled
                ? (playerTransform.position.y > ArenaBounds.center.y ? Vector2.down : Vector2.up)
                : Random.insideUnitCircle.normalized;
            Vector2 bossPos = ArenaBounds.Clamp((Vector2)playerTransform.position + (awayFromEdge * 9f), 3f);

            GameObject boss = Instantiate(fireBossPrefab, (Vector3)bossPos, Quaternion.identity);
            activeEnemies.Add(boss);
        }
    }
}
