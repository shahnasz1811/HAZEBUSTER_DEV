using UnityEngine;
using HazeBuster.Upgrades;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// Base class for all offensive tools and hydro-equipment in HAZEBUSTER.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Weapon Meta")]
        public string weaponName = "Base Weapon";
        public int weaponLevel = 1;
        public bool isUnlocked = true;

        [Header("Common Combat Stats")]
        public float baseDamage = 15f;
        public float damageMultiplier = 1f;
        public float baseFireRate = 2f; // Attacks per second
        public float fireRateMultiplier = 1f;

        protected float fireCooldownTimer = 0f;

        public float FinalDamage => baseDamage * damageMultiplier;
        public float FinalFireRate => baseFireRate * fireRateMultiplier;

        protected virtual void Update()
        {
            if (!isUnlocked) return;

            fireCooldownTimer -= Time.deltaTime;
            if (fireCooldownTimer <= 0f && CanFire())
            {
                Fire();
                fireCooldownTimer = 1f / Mathf.Max(0.1f, FinalFireRate);
            }
        }

        protected abstract bool CanFire();
        protected abstract void Fire();

        public abstract void ApplyUpgrade(UpgradeType upgradeType, float value);
    }
}
