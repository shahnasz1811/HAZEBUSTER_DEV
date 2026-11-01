using System;
using System.Collections.Generic;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Player;
using HazeBuster.Weapons;

namespace HazeBuster.Upgrades
{
    /// <summary>
    /// Manages the roguelite upgrade system: rolls 3 random valid cards upon level up,
    /// presents them to the player, and applies the selected perk to player/weapons.
    /// Works out-of-the-box with built-in procedural upgrade definitions!
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("ScriptableObject Upgrade Pool (Optional)")]
        public List<UpgradeData> customUpgradePool = new List<UpgradeData>();

        [Header("Runtime Upgrade Pool")]
        private List<UpgradeData> activePool = new List<UpgradeData>();

        [Header("Player & Weapon References")]
        public PlayerController playerController;
        public PlayerHealth playerHealth;
        public PlayerVision playerVision;
        public WaterGun waterGun;
        public PeatSprinkler peatSprinkler;
        public FoamBombLauncher foamBombLauncher;

        public event Action<List<UpgradeData>> OnUpgradeSelectionReady;

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
            FindReferencesIfNull();
            InitializeUpgradePool();
        }

        private void FindReferencesIfNull()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerController == null) playerController = player.GetComponent<PlayerController>();
                if (playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
                if (playerVision == null) playerVision = player.GetComponent<PlayerVision>();
                if (waterGun == null) waterGun = player.GetComponentInChildren<WaterGun>();
                if (peatSprinkler == null) peatSprinkler = player.GetComponentInChildren<PeatSprinkler>();
                if (foamBombLauncher == null) foamBombLauncher = player.GetComponentInChildren<FoamBombLauncher>();
            }
        }

        private void InitializeUpgradePool()
        {
            activePool.Clear();

            // If the user created custom UpgradeData assets in the project, use them
            if (customUpgradePool != null && customUpgradePool.Count > 0)
            {
                foreach (var up in customUpgradePool)
                {
                    UpgradeData clone = Instantiate(up);
                    clone.currentApplications = 0;
                    activePool.Add(clone);
                }
                return;
            }

            // Otherwise, populate default upgrades out-of-the-box!
            CreateBuiltInUpgrade("wg_firerate", "Hydro Rapid Valve", "Increases Water Gun firing speed by +25%.", UpgradeType.WaterGunFireRate, 0.25f, 5);
            CreateBuiltInUpgrade("wg_damage", "High-Pressure Jet", "Increases Water Gun droplet damage by +30%.", UpgradeType.WaterGunDamage, 0.30f, 5);
            CreateBuiltInUpgrade("wg_multishot", "Dual Stream Splitter", "Adds +1 additional water stream in a fan spread.", UpgradeType.WaterGunMultishot, 1f, 4);
            CreateBuiltInUpgrade("wg_size", "Mega Droplets", "Enlarges water projectiles by +40% for easier extinguishing.", UpgradeType.WaterGunSize, 0.40f, 4);
            CreateBuiltInUpgrade("wg_pierce", "Drilling Stream", "Water droplets pierce +1 additional burning enemy.", UpgradeType.WaterGunPierce, 1f, 3);

            CreateBuiltInUpgrade("vis_radius", "High-Beam Halogen", "Expands your vision circle through the haze by +2.0 units.", UpgradeType.VisionRadius, 2.0f, 4);

            CreateBuiltInUpgrade("sprink_unlock", "Deploy Peat Sprinkler", "Equips an automated 360-degree rotating sprinkler.", UpgradeType.UnlockSprinkler, 1f, 1, true);
            CreateBuiltInUpgrade("sprink_firerate", "Sprinkler Motor", "Increases Peat Sprinkler spin and spray speed by +30%.", UpgradeType.SprinklerFireRate, 0.30f, 4);
            CreateBuiltInUpgrade("sprink_nozzle", "Quad Nozzle Upgrade", "Adds +2 additional spray nozzles to the sprinkler.", UpgradeType.SprinklerNozzles, 2f, 3);

            CreateBuiltInUpgrade("foam_unlock", "Chemical Foam Mortar", "Equips a mortar that launches extinguishing foam canisters.", UpgradeType.UnlockFoamBomb, 1f, 1, true);
            CreateBuiltInUpgrade("foam_radius", "Expanding Agent", "Increases Foam Bomb puddle radius by +1.5 units.", UpgradeType.FoamBombRadius, 1.5f, 3);

            CreateBuiltInUpgrade("dash_hydro", "Hydro-Dash Trail", "Dashing leaves an extinguishing water puddle that quenches burning ground.", UpgradeType.DashHydroTrail, 1f, 1, true);
            CreateBuiltInUpgrade("dash_steam", "Steam Burst Exhaust", "Dashing releases a burst of steam that knocks back nearby embers.", UpgradeType.DashSteamBurst, 1f, 1, true);
            CreateBuiltInUpgrade("dash_charge", "Auxiliary Water Reservoir", "Grants +1 additional Dash charge for quick evasions.", UpgradeType.DashExtraCharge, 1f, 2);
            CreateBuiltInUpgrade("dash_cd", "Hydraulic Recoil Valve", "Reduces Dash cooldown by 20%.", UpgradeType.DashCooldown, 0.20f, 3);

            CreateBuiltInUpgrade("suit_heal", "Emergency Suit Coolant", "Instantly restores 40 suit integrity (HP).", UpgradeType.SuitHeal, 40f, 99);
        }

        private void CreateBuiltInUpgrade(string id, string title, string desc, UpgradeType type, float val, int maxApp, bool isUnlock = false)
        {
            UpgradeData data = ScriptableObject.CreateInstance<UpgradeData>();
            data.upgradeId = id;
            data.title = title;
            data.description = desc;
            data.upgradeType = type;
            data.value = val;
            data.maxApplications = maxApp;
            data.currentApplications = 0;
            data.isUnlockUpgrade = isUnlock;
            activePool.Add(data);
        }

        public void TriggerUpgradeSelection()
        {
            GameManager.Instance?.SetState(GameManager.GameState.LevelingUp);

            List<UpgradeData> validUpgrades = GetValidUpgrades();
            List<UpgradeData> selectedThree = new List<UpgradeData>();

            int countToPick = Mathf.Min(3, validUpgrades.Count);
            for (int i = 0; i < countToPick; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, validUpgrades.Count);
                selectedThree.Add(validUpgrades[randomIndex]);
                validUpgrades.RemoveAt(randomIndex);
            }

            OnUpgradeSelectionReady?.Invoke(selectedThree);
        }

        private List<UpgradeData> GetValidUpgrades()
        {
            List<UpgradeData> valid = new List<UpgradeData>();

            foreach (var up in activePool)
            {
                if (up.currentApplications >= up.maxApplications) continue;

                // Weapon dependency checks
                if (up.upgradeType == UpgradeType.SprinklerFireRate || up.upgradeType == UpgradeType.SprinklerNozzles || up.upgradeType == UpgradeType.SprinklerDamage)
                {
                    if (peatSprinkler == null || !peatSprinkler.isUnlocked) continue;
                }
                if (up.upgradeType == UpgradeType.FoamBombFireRate || up.upgradeType == UpgradeType.FoamBombRadius || up.upgradeType == UpgradeType.FoamBombDamage)
                {
                    if (foamBombLauncher == null || !foamBombLauncher.isUnlocked) continue;
                }

                valid.Add(up);
            }

            return valid;
        }

        public void ApplySelectedUpgrade(UpgradeData upgrade)
        {
            upgrade.currentApplications++;

            switch (upgrade.upgradeType)
            {
                case UpgradeType.WaterGunFireRate:
                case UpgradeType.WaterGunDamage:
                case UpgradeType.WaterGunMultishot:
                case UpgradeType.WaterGunSize:
                case UpgradeType.WaterGunPierce:
                    waterGun?.ApplyUpgrade(upgrade.upgradeType, upgrade.value);
                    break;

                case UpgradeType.VisionRadius:
                    playerVision?.ExpandVision(upgrade.value);
                    break;

                case UpgradeType.UnlockSprinkler:
                    if (peatSprinkler != null) peatSprinkler.isUnlocked = true;
                    break;
                case UpgradeType.SprinklerFireRate:
                case UpgradeType.SprinklerDamage:
                case UpgradeType.SprinklerNozzles:
                    peatSprinkler?.ApplyUpgrade(upgrade.upgradeType, upgrade.value);
                    break;

                case UpgradeType.UnlockFoamBomb:
                    if (foamBombLauncher != null) foamBombLauncher.isUnlocked = true;
                    break;
                case UpgradeType.FoamBombFireRate:
                case UpgradeType.FoamBombDamage:
                case UpgradeType.FoamBombRadius:
                    foamBombLauncher?.ApplyUpgrade(upgrade.upgradeType, upgrade.value);
                    break;

                case UpgradeType.DashHydroTrail:
                    if (playerController != null) playerController.hasHydroDash = true;
                    break;
                case UpgradeType.DashSteamBurst:
                    if (playerController != null) playerController.hasSteamBurst = true;
                    break;
                case UpgradeType.DashExtraCharge:
                    if (playerController != null)
                    {
                        playerController.maxDashCharges += Mathf.RoundToInt(upgrade.value);
                        playerController.currentDashCharges++;
                    }
                    break;
                case UpgradeType.DashCooldown:
                    if (playerController != null)
                    {
                        playerController.baseDashCooldown *= (1f - upgrade.value);
                        playerController.currentDashCooldown = playerController.baseDashCooldown;
                    }
                    break;

                case UpgradeType.SuitHeal:
                    playerHealth?.Heal(upgrade.value);
                    break;
            }

            // Resume gameplay
            GameManager.Instance?.SetState(GameManager.GameState.Playing);
        }
    }
}
