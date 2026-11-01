using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HazeBuster.Core
{
    /// <summary>
    /// Master controller for game state, run timer, score, and game over / victory flow.
    /// Designed for beginners: clean, well-commented, and easily accessible via GameManager.Instance.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Playing,
            LevelingUp,
            Paused,
            GameOver,
            Victory
        }

        [Header("State")]
        [Tooltip("Current state of the game loop.")]
        public GameState currentState = GameState.Playing;

        [Header("Survival Run Timer")]
        [Tooltip("Total run time required to summon the final Fire Boss (in seconds). E.g. 600 = 10 minutes.")]
        public float runDurationToBoss = 300f; // 5 minutes default for testing, scalable in Inspector
        public float currentRunTimer = 0f;
        public bool isBossSpawned = false;

        [Header("Session Stats")]
        public int totalDewCollected = 0;
        public int enemiesExtinguished = 0;

        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            SetState(GameState.Playing);
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                currentRunTimer += Time.deltaTime;

                // Check if time has come to summon the final Fire Boss
                if (currentRunTimer >= runDurationToBoss && !isBossSpawned)
                {
                    TriggerBossEncounter();
                }

                // Pause toggle
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    TogglePause();
                }
            }
            else if (currentState == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    TogglePause();
                }
            }
        }

        public void SetState(GameState newState)
        {
            currentState = newState;

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.LevelingUp:
                case GameState.Paused:
                case GameState.GameOver:
                case GameState.Victory:
                    Time.timeScale = 0f;
                    break;
            }

            OnGameStateChanged?.Invoke(currentState);
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        public void RegisterDewPickup(int amount)
        {
            totalDewCollected += amount;
        }

        public void RegisterEnemyDefeated()
        {
            enemiesExtinguished++;
        }

        private void TriggerBossEncounter()
        {
            isBossSpawned = true;
            Debug.Log("<color=orange>[HAZEBUSTER] Alert! The Mega Wildfire Boss has awakened!</color>");
            Enemies.EnemySpawner spawner = FindFirstObjectByType<Enemies.EnemySpawner>();
            if (spawner != null)
            {
                spawner.SpawnFireBoss();
            }
        }

        public void TriggerVictory()
        {
            Debug.Log("<color=green>[HAZEBUSTER] VICTORY! The Peatland Wildfire has been extinguished!</color>");
            // Award meta-currency
            MetaProgression.MetaProgressionManager.Instance?.AddEcoCredits(totalDewCollected + 100);
            SetState(GameState.Victory);
        }

        public void TriggerGameOver()
        {
            Debug.Log("<color=red>[HAZEBUSTER] Mission Failed. The haze consumed the peatland.</color>");
            // Save whatever credits the player harvested before falling
            MetaProgression.MetaProgressionManager.Instance?.AddEcoCredits(totalDewCollected);
            SetState(GameState.GameOver);
        }

        public void RestartRun()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
