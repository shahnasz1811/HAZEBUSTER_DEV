using System.Collections.Generic;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Player;
using HazeBuster.Boss;

namespace HazeBuster.Enemies
{
    /// <summary>
    /// Spawns fire hazards just outside the player's vision edge so they emerge from the haze.
    /// Manages escalating survival waves and summons the Fire Boss at the climax.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Prefabs")]
        public GameObject emberPrefab;
        public GameObject burnedStumpPrefab;
        public GameObject gambutPrefab;
        public GameObject fireBossPrefab;

        [Header("Spawn Settings")]
        public float spawnRadius = 10f; // Outside typical vision radius
        public float baseSpawnInterval = 1.8f;
        public float minSpawnInterval = 0.4f;
        public int maxEnemiesAlive = 60;

        private Transform playerTransform;
        private PlayerVision playerVision;
        private float spawnTimer = 0f;
        private List<GameObject> activeEnemies = new List<GameObject>();

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerVision = player.GetComponent<PlayerVision>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            {
                return;
            }

            // Clean up null references from defeated enemies
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
            float spawnDist = Mathf.Max(spawnRadius, currentVision + 2.5f);

            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDist;
            Vector3 spawnPos = playerTransform.position + (Vector3)randomCircle;

            // Pick enemy type based on run progress
            GameObject prefabToSpawn = emberPrefab;

            float roll = Random.value;
            if (runProgress > 0.6f) // Late run: Gambut & Stumps frequent
            {
                if (roll < 0.4f) prefabToSpawn = emberPrefab;
                else if (roll < 0.75f) prefabToSpawn = gambutPrefab;
                else prefabToSpawn = burnedStumpPrefab;
            }
            else if (runProgress > 0.25f) // Mid run: Burned Stumps introduced
            {
                if (roll < 0.65f) prefabToSpawn = emberPrefab;
                else prefabToSpawn = burnedStumpPrefab;
            }
            else // Early run: mostly fast embers
            {
                prefabToSpawn = emberPrefab;
            }

            if (prefabToSpawn != null)
            {
                GameObject enemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                activeEnemies.Add(enemy);
            }
        }

        public void SpawnFireBoss()
        {
            if (fireBossPrefab == null || playerTransform == null) return;

            Vector3 bossSpawnPos = playerTransform.position + (Vector3.up * 8f);
            GameObject boss = Instantiate(fireBossPrefab, bossSpawnPos, Quaternion.identity);
            activeEnemies.Add(boss);
        }
    }
}
