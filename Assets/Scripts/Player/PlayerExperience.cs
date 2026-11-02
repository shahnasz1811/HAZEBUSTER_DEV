using System;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Upgrades;
using HazeBuster.MetaProgression;

namespace HazeBuster.Player
{
    /// <summary>
    /// Tracks Morning Dew / EXP collection, handles level-ups, and manages the magnetic pickup range.
    /// Base pickup radius is tight (1.2m), and expands as you choose in-run or meta upgrades!
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        [Header("Level & EXP")]
        public int currentLevel = 1;
        public int currentExp = 0;
        public int expToNextLevel = 6;
        public float expGrowthFactor = 1.3f;

        [Header("Pickup / Magnet Radius")]
        [Tooltip("Tight base touch radius. Dew stays on ground until you enter this circle.")]
        public float baseMagnetRadius = 1.2f;
        public float inRunMagnetBonus = 0f;
        public float currentMagnetRadius;

        public event Action<int, int, int> OnExperienceChanged; // (currentExp, expToNext, currentLevel)

        private void Start()
        {
            ApplyMetaBonuses();
            OnExperienceChanged?.Invoke(currentExp, expToNextLevel, currentLevel);
        }

        public void ApplyMetaBonuses()
        {
            float bonusMagnet = MetaProgressionManager.Instance != null
                ? MetaProgressionManager.Instance.GetMagnetRadiusBonus()
                : 0f;
            currentMagnetRadius = baseMagnetRadius + bonusMagnet + inRunMagnetBonus;
        }

        public void ExpandPickupRadius(float amount)
        {
            inRunMagnetBonus += amount;
            currentMagnetRadius += amount;
            Debug.Log($"<color=cyan>[HAZEBUSTER] Dew Pickup Range expanded to {currentMagnetRadius:F1}m!</color>");
        }

        public void CollectDew(int amount)
        {
            currentExp += amount;
            GameManager.Instance?.RegisterDewPickup(amount);
            SoundManager.Instance?.PlayDewCollectSound();

            while (currentExp >= expToNextLevel)
            {
                LevelUp();
            }

            OnExperienceChanged?.Invoke(currentExp, expToNextLevel, currentLevel);
        }

        private void LevelUp()
        {
            currentExp -= expToNextLevel;
            currentLevel++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * expGrowthFactor) + 3;

            SoundManager.Instance?.PlayLevelUpSound();
            Debug.Log($"<color=cyan>[HAZEBUSTER] Level Up! Reached Level {currentLevel}</color>");

            // Trigger the Upgrade Selection UI & pauses the game!
            UpgradeManager.Instance?.TriggerUpgradeSelection();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, currentMagnetRadius);
        }
    }
}
