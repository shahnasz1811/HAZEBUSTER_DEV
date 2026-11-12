

using UnityEngine;
using HazeBuster.Upgrades;
using HazeBuster.Utility;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// Secondary weapon: An automated orbital sprinkler mounted to the player's tank.
    /// Continuously rotates and fires water droplets in a 360-degree radial pattern.
    /// </summary>
    public class PeatSprinkler : WeaponBase
    {
        [Header("Sprinkler Settings")]
        public GameObject dropletPrefab;
        public int nozzleCount = 4;
        public float rotationSpeed = 90f; // Degrees per second
        public float dropletSpeed = 8f;
        public float dropletSize = 0.8f;
        public int pierceCount = 1;

        private float currentAngle = 0f;

        private void Start()
        {
            weaponName = "Peat Sprinkler";
            baseDamage = 8f;
            baseFireRate = 3f;
        }

        protected override void Update()
        {
            base.Update();
            // Continuous rotation
            currentAngle += rotationSpeed * Time.deltaTime;
            if (currentAngle >= 360f) currentAngle -= 360f;
        }

        protected override bool CanFire()
        {
            // Automated weapon: fires automatically whenever ready
            return isUnlocked && dropletPrefab != null;
        }

        protected override void Fire()
        {
            float step = 360f / nozzleCount;

            for (int i = 0; i < nozzleCount; i++)
            {
                float angle = currentAngle + (i * step);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                GameObject obj = PoolManager.Spawn(dropletPrefab, transform.position, Quaternion.identity);
                WaterProjectile proj = obj.GetComponent<WaterProjectile>();
                if (proj != null)
                {
                    proj.Initialize(dir, FinalDamage, dropletSpeed, dropletSize, pierceCount, 3f);
                }
            }
        }

        public override void ApplyUpgrade(UpgradeType upgradeType, float value)
        {
            switch (upgradeType)
            {
                case UpgradeType.SprinklerFireRate:
                    fireRateMultiplier += value;
                    break;
                case UpgradeType.SprinklerDamage:
                    damageMultiplier += value;
                    break;
                case UpgradeType.SprinklerNozzles:
                    nozzleCount += Mathf.RoundToInt(value);
                    break;
            }
            weaponLevel++;
        }
    }
}
