using UnityEngine;
using HazeBuster.Upgrades;
using HazeBuster.Enemies;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// Secondary Weapon: Heavy mortar that launches chemical foam canisters
    /// to extinguish large patches of fire and blanket enemy clusters.
    /// </summary>
    public class FoamBombLauncher : WeaponBase
    {
        [Header("Launcher Settings")]
        public GameObject bombProjectilePrefab;
        public float targetingRange = 10f;
        public float blastRadius = 3.5f;
        public float puddleDuration = 5f;
        public float puddleDPS = 25f;
        public float directImpactDamage = 35f;

        private void Start()
        {
            weaponName = "Foam Bomb Mortar";
            baseDamage = directImpactDamage;
            baseFireRate = 0.4f; // 1 shot every 2.5 seconds
        }

        protected override bool CanFire()
        {
            return isUnlocked && bombProjectilePrefab != null && FindTargetPosition() != Vector3.zero;
        }

        protected override void Fire()
        {
            Vector3 target = FindTargetPosition();
            if (target == Vector3.zero) return;

            GameObject bomb = Instantiate(bombProjectilePrefab, transform.position, Quaternion.identity);
            FoamBombProjectile proj = bomb.GetComponent<FoamBombProjectile>();
            if (proj != null)
            {
                proj.Initialize(target, 0.75f, blastRadius, puddleDuration, puddleDPS * damageMultiplier, FinalDamage);
            }
        }

        private Vector3 FindTargetPosition()
        {
            // Find nearest enemy to bomb
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, targetingRange);
            float closestDist = Mathf.Infinity;
            Vector3 bestPos = Vector3.zero;

            foreach (var col in colliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    float dist = Vector2.Distance(transform.position, col.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestPos = col.transform.position;
                    }
                }
            }

            // If no enemy found, aim towards mouse
            if (bestPos == Vector3.zero && Camera.main != null)
            {
                // Where the cursor points on the ground (a tilted camera cannot use ScreenToWorldPoint for this).
                Vector3 mouse = HazeBuster.Utility.SlantedPerspective.ScreenToGround(Camera.main, HazeBuster.Utility.InputHelper.GetMousePosition(), transform.position);
                mouse.z = 0f;
                if (Vector2.Distance(transform.position, mouse) <= targetingRange)
                {
                    bestPos = mouse;
                }
            }

            return bestPos;
        }

        public override void ApplyUpgrade(UpgradeType upgradeType, float value)
        {
            switch (upgradeType)
            {
                case UpgradeType.FoamBombFireRate:
                    fireRateMultiplier += value;
                    break;
                case UpgradeType.FoamBombDamage:
                    damageMultiplier += value;
                    break;
                case UpgradeType.FoamBombRadius:
                    blastRadius += value;
                    break;
            }
            weaponLevel++;
        }
    }
}
