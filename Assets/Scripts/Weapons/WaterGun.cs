using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Upgrades;
using HazeBuster.MetaProgression;
using HazeBuster.Utility;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// The primary weapon of the Scientist Engineer.
    /// Fires pressurized water droplets towards the mouse cursor.
    /// Fully upgradeable in fire rate, multishot/spread, projectile size, and pierce.
    /// Uses InputHelper so mouse aiming and firing work in any input mode.
    /// </summary>
    public class WaterGun : WeaponBase
    {
        [Header("Water Gun Specifics")]
        public GameObject projectilePrefab;
        public Transform firePoint;
        public Camera mainCamera;

        [Header("Upgradable Parameters")]
        public int projectileCount = 1;
        public float spreadAngle = 12f;
        public float projectileSizeMultiplier = 1f;
        public float projectileSpeed = 14f;
        public int pierceCount = 1;
        public float knockback = 6f;

        private void Start()
        {
            weaponName = "Hydro-Blaster Gun";
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (firePoint == null)
            {
                firePoint = transform;
            }

            // Apply meta-progression bonus to fire rate
            float metaFireRateBonus = MetaProgressionManager.Instance != null
                ? MetaProgressionManager.Instance.GetWaterFireRateBonus()
                : 0f;
            baseFireRate += metaFireRateBonus;
        }

        protected override bool CanFire()
        {
            // Fires when Left Mouse Button is held down via universal InputHelper
            return InputHelper.IsFireHeld();
        }

        protected override void Fire()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (projectilePrefab == null) return;

            // Where the cursor points on the ground (a tilted camera cannot use ScreenToWorldPoint for this).
            Vector2 mouseGroundPos = SlantedPerspective.ScreenToGround(mainCamera, InputHelper.GetMousePosition(), firePoint.position);
            Vector2 baseDirection = (mouseGroundPos - (Vector2)firePoint.position).normalized;

            // Multi-shot fan calculations
            float startAngle = -(spreadAngle * (projectileCount - 1)) / 2f;

            for (int i = 0; i < projectileCount; i++)
            {
                float currentAngle = startAngle + (i * spreadAngle);
                Vector2 dir = RotateVector(baseDirection, currentAngle);

                GameObject projObj = PoolManager.Spawn(projectilePrefab, firePoint.position, Quaternion.identity);
                WaterProjectile proj = projObj.GetComponent<WaterProjectile>();
                if (proj != null)
                {
                    proj.Initialize(
                        dir,
                        FinalDamage,
                        projectileSpeed,
                        projectileSizeMultiplier,
                        pierceCount,
                        knockback
                    );
                }
            }

            SoundManager.Instance?.PlayShootSound();
        }

        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
            return new Vector2((cos * v.x) - (sin * v.y), (sin * v.x) + (cos * v.y));
        }

        public override void ApplyUpgrade(UpgradeType upgradeType, float value)
        {
            switch (upgradeType)
            {
                case UpgradeType.WaterGunFireRate:
                    fireRateMultiplier += value;
                    break;
                case UpgradeType.WaterGunDamage:
                    damageMultiplier += value;
                    break;
                case UpgradeType.WaterGunMultishot:
                    projectileCount += Mathf.RoundToInt(value);
                    break;
                case UpgradeType.WaterGunSize:
                    projectileSizeMultiplier += value;
                    break;
                case UpgradeType.WaterGunPierce:
                    pierceCount += Mathf.RoundToInt(value);
                    break;
            }
            weaponLevel++;
        }
    }
}
