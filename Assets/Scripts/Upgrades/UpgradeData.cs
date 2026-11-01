using UnityEngine;

namespace HazeBuster.Upgrades
{
    /// <summary>
    /// ScriptableObject defining an in-run upgrade card.
    /// Create these in the Unity Project via Right Click -> Create -> HazeBuster -> Upgrade Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "HazeBuster/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("Card Information")]
        public string upgradeId;
        public string title;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Mechanic")]
        public UpgradeType upgradeType;
        [Tooltip("The numerical bonus applied (e.g. 0.25 = +25% fire rate, 1 = +1 projectile, 1.5 = +1.5 vision radius)")]
        public float value = 1f;

        [Header("Availability & Rules")]
        public bool isUnlockUpgrade = false; // e.g. Unlocking Sprinkler or Foam Bomb
        public int maxApplications = 5; // How many times this upgrade can be chosen in a single run
        [HideInInspector] public int currentApplications = 0;
    }
}
