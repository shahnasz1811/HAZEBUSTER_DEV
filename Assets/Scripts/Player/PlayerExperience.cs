using System;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Upgrades;
using HazeBuster.MetaProgression;

namespace HazeBuster.Player
{
    /// <summary>
    /// Tracks Morning Dew / EXP collection, handles level-ups, and manages the magnetic attraction of dew drops.
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        [Header("Level & EXP")]
        public int currentLevel = 1;
        public int currentExp = 0;
        public int expToNextLevel = 5;
        public float expGrowthFactor = 1.35f;

        [Header("Magnet Radius")]
        public float baseMagnetRadius = 3f;
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
            currentMagnetRadius = baseMagnetRadius + bonusMagnet;
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
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * expGrowthFactor) + 2;

            SoundManager.Instance?.PlayLevelUpSound();
            Debug.Log($"<color=cyan>[HAZEBUSTER] Level Up! Reached Level {currentLevel}</color>");

            // Trigger the Upgrade Selection UI
            UpgradeManager.Instance?.TriggerUpgradeSelection();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentMagnetRadius);
        }
    }
}
