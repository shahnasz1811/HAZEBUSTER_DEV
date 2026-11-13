using System.Collections;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.Enemies;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// Canister launched by the Foam Bomb Launcher.
    /// Arcs through the air and detonates upon landing into an extinguishing chemical foam zone.
    /// </summary>
    public class FoamBombProjectile : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject foamPuddlePrefab;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float flightDuration;
        private float puddleDuration;
        private float explosionRadius;
        private float damagePerSecond;
        private float impactDamage;

        public void Initialize(Vector3 target, float duration, float blastRadius, float puddleLifetime, float dps, float impactDmg)
        {
            startPosition = transform.position;
            targetPosition = target;
            flightDuration = duration;
            explosionRadius = blastRadius;
            puddleDuration = puddleLifetime;
            damagePerSecond = dps;
            impactDamage = impactDmg;

            StartCoroutine(ArcRoutine());
        }

        private IEnumerator ArcRoutine()
        {
            float elapsed = 0f;
            float arcHeight = 2.5f;

            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration);

                // Parabolic trajectory
                Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = currentPos;

                // Tumble rotation
                transform.Rotate(0, 0, 720f * Time.deltaTime);

                yield return null;
            }

            Detonate();
        }

        private void Detonate()
        {
            SoundManager.Instance?.PlayExplosionSound();

            // Direct damage & knockback on impact
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var col in enemies)
            {
                if (col.CompareTag("Enemy"))
                {
                    EnemyBase enemy = col.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeWaterDamage(impactDamage);
                        Vector2 knockback = (enemy.transform.position - transform.position).normalized;
                        enemy.ApplyKnockback(knockback, 10f);
                    }
                }
            }

            // Spawn lingering foam puddle
            if (foamPuddlePrefab != null)
            {
                GameObject puddle = Instantiate(foamPuddlePrefab, transform.position, Quaternion.identity);
                FoamPuddle fp = puddle.GetComponent<FoamPuddle>();
                if (fp == null)
                {
                    // The puddle prefab has no FoamPuddle script, so add it: without it nothing
                    // initialises, damages or removes the puddle and it stays forever.
                    fp = puddle.AddComponent<FoamPuddle>();
                }
                Destroy(puddle, puddleDuration + 1f); // safety net
                if (fp != null)
                {
                    fp.Initialize(puddleDuration, explosionRadius, damagePerSecond);
                }
            }

            Destroy(gameObject);
        }
    }
}
