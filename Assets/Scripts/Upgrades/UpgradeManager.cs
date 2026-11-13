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
    /// presents them to the player, pauses gameplay, and applies the selected perk.
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
        public PlayerExperience playerExperience;
        public WaterGun waterGun;
        public PeatSprinkler peatSprinkler;
        public FoamBombLauncher foamBombLauncher;

        public event Action<List<UpgradeData>> OnUpgradeSelectionReady;

        // Level-ups waiting for a card choice. One big dew pickup can cause several level-ups at once.
        private int pendingSelections = 0;
        private bool selectionOpen = false;

        private void Awake()
        {
            // Scene-scoped singleton ("latest wins"): a Retry reloads the scene with a fresh UpgradeManager.
            if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            FindReferencesIfNull();
            InitializeUpgradePool();
            LockSecondaryWeapons();
        }

        /// <summary>
        /// The Peat Sprinkler and Foam Mortar start locked and switched off. A weapon component that is
        /// disabled never runs Update, so the unlock card must switch it on (see ApplySelectedUpgrade).
        /// </summary>
        private void LockSecondaryWeapons()
        {
            if (peatSprinkler != null)
            {
                peatSprinkler.isUnlocked = false;
                peatSprinkler.enabled = false;
            }
            if (foamBombLauncher != null)
            {
                foamBombLauncher.isUnlocked = false;
                foamBombLauncher.enabled = false;
            }
        }

        private void FindReferencesIfNull()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerController == null) playerController = player.GetComponent<PlayerController>();
                if (playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
                if (playerVision == null) playerVision = player.GetComponent<PlayerVision>();
                if (playerExperience == null) playerExperience = player.GetComponent<PlayerExperience>();
                if (waterGun == null) waterGun = player.GetComponentInChildren<WaterGun>();
                if (peatSprinkler == null) peatSprinkler = player.GetComponentInChildren<PeatSprinkler>();
                if (foamBombLauncher == null) foamBombLauncher = player.GetComponentInChildren<FoamBombLauncher>();
            }
        }

        private void InitializeUpgradePool()
        {
            activePool.Clear();

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

            // Default balanced built-in upgrades:
            // 1. Water Gun Primary Upgrades
            CreateBuiltInUpgrade("wg_firerate", "Rapid Pressure Valve", "Increases Water Gun firing speed by +25%.", UpgradeType.WaterGunFireRate, 0.25f, 5);
            CreateBuiltInUpgrade("wg_damage", "High-Velocity Jet", "Increases Water Gun droplet damage by +30%.", UpgradeType.WaterGunDamage, 0.30f, 5);
            CreateBuiltInUpgrade("wg_multishot", "Dual Stream Splitter", "Adds +1 additional water stream in a fan spread.", UpgradeType.WaterGunMultishot, 1f, 4);
            CreateBuiltInUpgrade("wg_size", "Mega Water Bolts", "Enlarges water projectiles by +40% for easier dousing.", UpgradeType.WaterGunSize, 0.40f, 4);
            CreateBuiltInUpgrade("wg_pierce", "Drilling Stream", "Water droplets pierce through +1 additional burning enemy.", UpgradeType.WaterGunPierce, 1f, 3);

            // 2. Vision & Utility
            CreateBuiltInUpgrade("vis_radius", "Halogen High-Beam", "Expands your vision light circle through the haze by +2.0m.", UpgradeType.VisionRadius, 2.0f, 4);
            CreateBuiltInUpgrade("pickup_range", "Hydro-Vacuum Tank", "Expands Morning Dew pickup magnet range by +1.2m.", UpgradeType.PickupRange, 1.2f, 4);
            CreateBuiltInUpgrade("move_speed", "Pneumatic Boots", "Increases Scientist movement speed by +15%.", UpgradeType.PlayerSpeed, 0.15f, 4);
            CreateBuiltInUpgrade("max_hp", "Reinforced Hazmat Lining", "Increases Max Health by +30 and heals 30 HP.", UpgradeType.MaxHealth, 30f, 4);

            // 3. Secondary Weapons
            CreateBuiltInUpgrade("sprink_unlock", "Deploy Peat Sprinkler", "Equips an automated 360-degree rotating sprinkler.", UpgradeType.UnlockSprinkler, 1f, 1, true);
            CreateBuiltInUpgrade("sprink_firerate", "Sprinkler Motor Overclock", "Increases Peat Sprinkler spin and spray speed by +30%.", UpgradeType.SprinklerFireRate, 0.30f, 4);
            CreateBuiltInUpgrade("sprink_nozzle", "Quad Nozzle Manifold", "Adds +2 additional spray nozzles to the sprinkler.", UpgradeType.SprinklerNozzles, 2f, 3);

            CreateBuiltInUpgrade("foam_unlock", "Chemical Foam Mortar", "Equips a mortar that launches extinguishing foam canisters.", UpgradeType.UnlockFoamBomb, 1f, 1, true);
            CreateBuiltInUpgrade("foam_radius", "Expanding Chemical Agent", "Increases Foam Bomb puddle radius by +1.5m.", UpgradeType.FoamBombRadius, 1.5f, 3);

            // 4. Dash Evolutions
            CreateBuiltInUpgrade("dash_hydro", "Hydro-Dash Trail", "Dashing leaves an extinguishing water puddle that douses burning ground.", UpgradeType.DashHydroTrail, 1f, 1, true);
            CreateBuiltInUpgrade("dash_steam", "Steam Burst Exhaust", "Dashing releases a blast of steam that knocks back nearby embers.", UpgradeType.DashSteamBurst, 1f, 1, true);
            CreateBuiltInUpgrade("dash_charge", "Auxiliary Water Reservoir", "Grants +1 additional Dash charge for quick evasions.", UpgradeType.DashExtraCharge, 1f, 2);
            CreateBuiltInUpgrade("dash_cd", "Hydraulic Recoil Valve", "Reduces Dash cooldown by 20%.", UpgradeType.DashCooldown, 0.20f, 3);

            // 5. Emergency Maintenance
            CreateBuiltInUpgrade("suit_heal", "Emergency Suit Coolant", "Instantly restores 45 suit integrity (HP).", UpgradeType.SuitHeal, 45f, 99);
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
            // Each level-up is queued and its 3 cards are shown one after the other.
            pendingSelections++;
            if (!selectionOpen)
            {
                OpenNextSelection();
            }
        }

        private void OpenNextSelection()
        {
            // Pause the entire game!
            GameManager.Instance?.SetState(GameManager.GameState.LevelingUp);

            List<UpgradeData> validUpgrades = GetValidUpgrades();
            if (validUpgrades.Count == 0)
            {
                // Nothing left to offer: skip the queue instead of showing an empty menu.
                pendingSelections = 0;
                selectionOpen = false;
                GameManager.Instance?.SetState(GameManager.GameState.Playing);
                return;
            }
            selectionOpen = true;
            List<UpgradeData> selectedThree = new List<UpgradeData>();

            int countToPick = Mathf.Min(3, validUpgrades.Count);
            for (int i = 0; i < countToPick; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, validUpgrades.Count);
                selectedThree.Add(validUpgrades[randomIndex]);
                validUpgrades.RemoveAt(randomIndex);
            }

            // Safety net: if nothing is listening, no menu can ever be shown and the paused game would never resume.
            if (OnUpgradeSelectionReady == null)
            {
                Debug.LogError("[HAZEBUSTER] Nothing is listening for level-ups (no UIManager in the scene?). The first upgrade is picked automatically so the game does not freeze.");
                ApplySelectedUpgrade(selectedThree[0]);
                return;
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

            FindReferencesIfNull();

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

                case UpgradeType.PickupRange:
                    playerExperience?.ExpandPickupRadius(upgrade.value);
                    break;

                case UpgradeType.PlayerSpeed:
                    if (playerController != null)
                    {
                        playerController.baseMoveSpeed *= (1f + upgrade.value);
                        playerController.ApplyMetaBonuses(); // recalculates speed and keeps the permanent Lab bonus
                    }
                    break;

                case UpgradeType.MaxHealth:
                    if (playerHealth != null)
                    {
                        playerHealth.maxHealth += upgrade.value;
                        playerHealth.Heal(upgrade.value);
                    }
                    break;

                case UpgradeType.UnlockSprinkler:
                    if (peatSprinkler != null)
                    {
                        peatSprinkler.isUnlocked = true;
                        peatSprinkler.enabled = true; // a disabled weapon never fires
                    }
                    break;
                case UpgradeType.SprinklerFireRate:
                case UpgradeType.SprinklerDamage:
                case UpgradeType.SprinklerNozzles:
                    peatSprinkler?.ApplyUpgrade(upgrade.upgradeType, upgrade.value);
                    break;

                case UpgradeType.UnlockFoamBomb:
                    if (foamBombLauncher != null)
                    {
                        foamBombLauncher.isUnlocked = true;
                        foamBombLauncher.enabled = true; // a disabled weapon never fires
                    }
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
                        playerController.ApplyMetaBonuses(); // recalculates cooldown and keeps the permanent Lab bonus
                    }
                    break;

                case UpgradeType.SuitHeal:
                    playerHealth?.Heal(upgrade.value);
                    break;
            }

            // Show the next queued level-up, or unpause and resume gameplay.
            pendingSelections = Mathf.Max(0, pendingSelections - 1);
            if (pendingSelections > 0)
            {
                OpenNextSelection();
                return;
            }
            selectionOpen = false;
            GameManager.Instance?.SetState(GameManager.GameState.Playing);
        }
    }
}
