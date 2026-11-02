using System;
using UnityEngine;

namespace HazeBuster.MetaProgression
{
    /// <summary>
    /// Manages persistent meta-progression saved between runs using PlayerPrefs.
    /// Tracks permanent Eco-Credits collected and unlocks base stat upgrades:
    /// Move Speed, Max Health, Dash Cooldown, Water Fire Rate, Vision Reach, and Dew Magnet.
    /// </summary>
    public class MetaProgressionManager : MonoBehaviour
    {
        public static MetaProgressionManager Instance { get; private set; }

        private const string KEY_ECO_CREDITS = "HB_EcoCredits";
        private const string KEY_MOVE_SPEED_LVL = "HB_MoveSpeed_Lvl";
        private const string KEY_MAX_HEALTH_LVL = "HB_MaxHealth_Lvl";
        private const string KEY_DASH_CD_LVL = "HB_DashCD_Lvl";
        private const string KEY_WATER_FR_LVL = "HB_WaterFR_Lvl";
        private const string KEY_VISION_LVL = "HB_Vision_Lvl";
        private const string KEY_MAGNET_LVL = "HB_Magnet_Lvl";

        [Header("Persistent Currency")]
        public int totalEcoCredits = 0;

        [Header("Meta Levels (0 to 5)")]
        public int moveSpeedLvl = 0;
        public int maxHealthLvl = 0;
        public int dashCdLvl = 0;
        public int waterFrLvl = 0;
        public int visionLvl = 0;
        public int magnetLvl = 0;

        public const int MAX_META_LEVEL = 5;

        public event Action OnMetaCreditsChanged;

        private void Awake()
        {
            // Scene-scoped singleton ("latest wins"). Saved progress lives in PlayerPrefs, so a fresh manager
            // after a Retry simply reloads it. DontDestroyOnLoad was removed on purpose: this component shares
            // the _GameManagers object with the GameManager, spawner and upgrade manager, and keeping that
            // object alive carried a stale run into the next one.
            if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadProgress();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadProgress()
        {
            totalEcoCredits = PlayerPrefs.GetInt(KEY_ECO_CREDITS, 0);
            moveSpeedLvl = PlayerPrefs.GetInt(KEY_MOVE_SPEED_LVL, 0);
            maxHealthLvl = PlayerPrefs.GetInt(KEY_MAX_HEALTH_LVL, 0);
            dashCdLvl = PlayerPrefs.GetInt(KEY_DASH_CD_LVL, 0);
            waterFrLvl = PlayerPrefs.GetInt(KEY_WATER_FR_LVL, 0);
            visionLvl = PlayerPrefs.GetInt(KEY_VISION_LVL, 0);
            magnetLvl = PlayerPrefs.GetInt(KEY_MAGNET_LVL, 0);
            OnMetaCreditsChanged?.Invoke();
        }

        public void SaveProgress()
        {
            PlayerPrefs.SetInt(KEY_ECO_CREDITS, totalEcoCredits);
            PlayerPrefs.SetInt(KEY_MOVE_SPEED_LVL, moveSpeedLvl);
            PlayerPrefs.SetInt(KEY_MAX_HEALTH_LVL, maxHealthLvl);
            PlayerPrefs.SetInt(KEY_DASH_CD_LVL, dashCdLvl);
            PlayerPrefs.SetInt(KEY_WATER_FR_LVL, waterFrLvl);
            PlayerPrefs.SetInt(KEY_VISION_LVL, visionLvl);
            PlayerPrefs.SetInt(KEY_MAGNET_LVL, magnetLvl);
            PlayerPrefs.Save();
            OnMetaCreditsChanged?.Invoke();
        }

        public void AddEcoCredits(int amount)
        {
            totalEcoCredits += amount;
            SaveProgress();
        }

        public int GetUpgradeCost(int currentLevel)
        {
            // Scaling cost: 20 -> 50 -> 100 -> 180 -> 300
            int[] costs = { 20, 50, 100, 180, 300 };
            if (currentLevel >= MAX_META_LEVEL) return 999999;
            return costs[Mathf.Clamp(currentLevel, 0, costs.Length - 1)];
        }

        public bool TryPurchaseUpgrade(string upgradeName)
        {
            int currentLvl = 0;
            switch (upgradeName)
            {
                case "MoveSpeed": currentLvl = moveSpeedLvl; break;
                case "MaxHealth": currentLvl = maxHealthLvl; break;
                case "DashCooldown": currentLvl = dashCdLvl; break;
                case "WaterFireRate": currentLvl = waterFrLvl; break;
                case "VisionRadius": currentLvl = visionLvl; break;
                case "MagnetRadius": currentLvl = magnetLvl; break;
                default: return false;
            }

            if (currentLvl >= MAX_META_LEVEL) return false;

            int cost = GetUpgradeCost(currentLvl);
            if (totalEcoCredits >= cost)
            {
                totalEcoCredits -= cost;
                switch (upgradeName)
                {
                    case "MoveSpeed": moveSpeedLvl++; break;
                    case "MaxHealth": maxHealthLvl++; break;
                    case "DashCooldown": dashCdLvl++; break;
                    case "WaterFireRate": waterFrLvl++; break;
                    case "VisionRadius": visionLvl++; break;
                    case "MagnetRadius": magnetLvl++; break;
                }
                SaveProgress();
                return true;
            }

            return false;
        }

        // --- Bonus Calculations applied in run ---
        public float GetMoveSpeedBonus() => moveSpeedLvl * 0.35f;
        public float GetMaxHealthBonus() => maxHealthLvl * 20f;
        public float GetDashCooldownReduction() => dashCdLvl * 0.15f;
        public float GetWaterFireRateBonus() => waterFrLvl * 0.4f;
        public float GetVisionRadiusBonus() => visionLvl * 0.8f;
        public float GetMagnetRadiusBonus() => magnetLvl * 0.8f;

        [ContextMenu("Reset All Save Data")]
        public void ResetSaveData()
        {
            // Only remove this game's own keys instead of wiping every PlayerPrefs entry.
            PlayerPrefs.DeleteKey(KEY_ECO_CREDITS);
            PlayerPrefs.DeleteKey(KEY_MOVE_SPEED_LVL);
            PlayerPrefs.DeleteKey(KEY_MAX_HEALTH_LVL);
            PlayerPrefs.DeleteKey(KEY_DASH_CD_LVL);
            PlayerPrefs.DeleteKey(KEY_WATER_FR_LVL);
            PlayerPrefs.DeleteKey(KEY_VISION_LVL);
            PlayerPrefs.DeleteKey(KEY_MAGNET_LVL);
            PlayerPrefs.Save();
            LoadProgress();
            Debug.Log("[HAZEBUSTER] All meta-progression reset to zero.");
        }
    }
}
