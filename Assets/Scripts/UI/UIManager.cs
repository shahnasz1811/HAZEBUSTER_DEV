using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HazeBuster.Core;
using HazeBuster.Player;
using HazeBuster.Upgrades;
using HazeBuster.Boss;
using HazeBuster.MetaProgression;

namespace HazeBuster.UI
{
    /// <summary>
    /// Master UI Manager for HAZEBUSTER.
    /// Manages the HUD (Health, EXP, Survival Clock, Dash), Level-Up 3-Card Modal,
    /// Game Over screen, Victory screen, Boss Health bar, and the Meta-Progression Shop.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Player References")]
        public PlayerHealth playerHealth;
        public PlayerExperience playerExperience;
        public PlayerController playerController;

        [Header("HUD Elements")]
        public Slider healthSlider;
        public Text healthText;
        public Slider expSlider;
        public Text levelText;
        public Text timerText;
        public Text dewCountText;
        public Text dashText;

        [Header("Boss HUD")]
        public GameObject bossBarContainer;
        public Slider bossHealthSlider;
        public Text bossNameText;

        [Header("Level Up Modal")]
        public GameObject levelUpPanel;
        public UpgradeCardUI[] upgradeCards = new UpgradeCardUI[3];

        [Header("Game Over & Victory Panels")]
        public GameObject gameOverPanel;
        public Text gameOverCreditsText;
        public Button gameOverRestartBtn;

        public GameObject victoryPanel;
        public Text victoryCreditsText;
        public Button victoryRestartBtn;

        [Header("Meta-Progression Shop Panel (Optional Hub)")]
        public GameObject metaShopPanel;
        public Text shopEcoCreditsText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindPlayerReferences();

            // Hook into player events
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(playerHealth.currentHealth, playerHealth.maxHealth);
            }
            if (playerExperience != null)
            {
                playerExperience.OnExperienceChanged += UpdateExpBar;
                UpdateExpBar(playerExperience.currentExp, playerExperience.expToNextLevel, playerExperience.currentLevel);
            }

            // Hook into UpgradeManager
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnUpgradeSelectionReady += ShowLevelUpPanel;
            }

            // Hook into GameManager state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            // Button listeners
            if (gameOverRestartBtn != null)
            {
                gameOverRestartBtn.onClick.AddListener(() => GameManager.Instance?.RestartRun());
            }
            if (victoryRestartBtn != null)
            {
                victoryRestartBtn.onClick.AddListener(() => GameManager.Instance?.RestartRun());
            }

            // Initial panel states
            if (levelUpPanel != null) levelUpPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (bossBarContainer != null) bossBarContainer.SetActive(false);
            if (metaShopPanel != null) metaShopPanel.SetActive(false);
        }

        private void FindPlayerReferences()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
                if (playerExperience == null) playerExperience = player.GetComponent<PlayerExperience>();
                if (playerController == null) playerController = player.GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            UpdateHUDTimer();
            UpdateDashIndicator();
            UpdateDewCount();
            CheckBossHUD();
        }

        private void UpdateHUDTimer()
        {
            if (timerText != null && GameManager.Instance != null)
            {
                float t = GameManager.Instance.currentRunTimer;
                int minutes = Mathf.FloorToInt(t / 60f);
                int seconds = Mathf.FloorToInt(t % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        private void UpdateDashIndicator()
        {
            if (dashText != null && playerController != null)
            {
                dashText.text = $"Dash: {playerController.currentDashCharges} / {playerController.maxDashCharges}";
            }
        }

        private void UpdateDewCount()
        {
            if (dewCountText != null && GameManager.Instance != null)
            {
                dewCountText.text = $"Dew: {GameManager.Instance.totalDewCollected}";
            }
        }

        private void UpdateHealthBar(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Clamp01(current / max);
            }
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void UpdateExpBar(int currentExp, int expToNext, int level)
        {
            if (expSlider != null)
            {
                expSlider.value = Mathf.Clamp01((float)currentExp / expToNext);
            }
            if (levelText != null)
            {
                levelText.text = $"Lv. {level}";
            }
        }

        private void CheckBossHUD()
        {
            if (FireBoss.Instance != null)
            {
                if (bossBarContainer != null && !bossBarContainer.activeSelf)
                {
                    bossBarContainer.SetActive(true);
                    if (bossNameText != null) bossNameText.text = FireBoss.Instance.enemyName;
                }
                if (bossHealthSlider != null)
                {
                    bossHealthSlider.value = Mathf.Clamp01(FireBoss.Instance.currentHealth / FireBoss.Instance.maxHealth);
                }
            }
            else
            {
                if (bossBarContainer != null && bossBarContainer.activeSelf)
                {
                    bossBarContainer.SetActive(false);
                }
            }
        }

        private void ShowLevelUpPanel(List<UpgradeData> upgrades)
        {
            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(true);

                for (int i = 0; i < upgradeCards.Length; i++)
                {
                    if (i < upgrades.Count && upgradeCards[i] != null)
                    {
                        upgradeCards[i].gameObject.SetActive(true);
                        upgradeCards[i].SetupCard(upgrades[i]);
                    }
                    else if (upgradeCards[i] != null)
                    {
                        upgradeCards[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void HandleGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.Playing)
            {
                if (levelUpPanel != null) levelUpPanel.SetActive(false);
            }
            else if (state == GameManager.GameState.GameOver)
            {
                if (gameOverPanel != null)
                {
                    gameOverPanel.SetActive(true);
                    if (gameOverCreditsText != null && GameManager.Instance != null)
                    {
                        gameOverCreditsText.text = $"Dew Harvested: +{GameManager.Instance.totalDewCollected} Eco-Credits";
                    }
                }
            }
            else if (state == GameManager.GameState.Victory)
            {
                if (victoryPanel != null)
                {
                    victoryPanel.SetActive(true);
                    if (victoryCreditsText != null && GameManager.Instance != null)
                    {
                        victoryCreditsText.text = $"Wildfire Defeated! +{GameManager.Instance.totalDewCollected + 100} Eco-Credits";
                    }
                }
            }
        }

        // --- Meta Shop Interaction ---
        public void ToggleMetaShop()
        {
            if (metaShopPanel != null)
            {
                bool active = !metaShopPanel.activeSelf;
                metaShopPanel.SetActive(active);
                RefreshMetaShopUI();
            }
        }

        public void RefreshMetaShopUI()
        {
            if (shopEcoCreditsText != null && MetaProgressionManager.Instance != null)
            {
                shopEcoCreditsText.text = $"Eco-Credits: {MetaProgressionManager.Instance.totalEcoCredits}";
            }
        }

        public void BuyMetaUpgrade(string upgradeName)
        {
            if (MetaProgressionManager.Instance != null)
            {
                if (MetaProgressionManager.Instance.TryPurchaseUpgrade(upgradeName))
                {
                    RefreshMetaShopUI();
                    Debug.Log($"[MetaShop] Purchased {upgradeName}!");
                }
            }
        }
    }
}
