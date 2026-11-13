using System.Collections;
using UnityEngine;
using HazeBuster.Environment;
using HazeBuster.Enemies;

namespace HazeBuster.Weapons
{
    /// <summary>
    /// Created when a Foam Bomb detonates.
    /// Spreads an expanding chemical foam circle that douses burning ground and drenches enemies.
    /// </summary>
    public class FoamPuddle : MonoBehaviour
    {
        [Header("Puddle Stats")]
        public float duration = 5f;
        public float damagePerSecond = 25f;
        public float slowMultiplier = 0.4f;
        public float maxRadius = 3f;

        private SpriteRenderer sr;
        private CircleCollider2D col;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
            }
        }

        public void Initialize(float puddleDuration, float radius, float dps)
        {
            duration = puddleDuration;
            maxRadius = radius;
            damagePerSecond = dps;
            // The collider radius is in local units and the puddle is scaled up to maxRadius,
            // so use half the sprite width here: the world radius then equals maxRadius.
            col.radius = (sr != null && sr.sprite != null ? sr.sprite.bounds.size.x : 1f) * 0.5f;

            StartCoroutine(PuddleLifecycle());
        }

        private IEnumerator PuddleLifecycle()
        {
            // Rapid expansion animation
            float expandTime = 0.3f;
            float elapsed = 0f;
            float spriteWidth = (sr != null && sr.sprite != null) ? sr.sprite.bounds.size.x : 1f;
            Vector3 targetScale = Vector3.one * (maxRadius * 2f / Mathf.Max(0.01f, spriteWidth));

            while (elapsed < expandTime)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / expandTime);
                yield return null;
            }
            transform.localScale = targetScale;

            // Douse immediate surroundings
            ExtinguishOverlappingGround();

            // Persist
            yield return new WaitForSeconds(duration - expandTime - 0.5f);

            // Fade out
            elapsed = 0f;
            Color initialColor = sr != null ? sr.color : Color.white;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                if (sr != null)
                {
                    sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, 1f - (elapsed / 0.5f));
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void ExtinguishOverlappingGround()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, maxRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Hazard") || hit.GetComponent<BurningGround>() != null)
                {
                    hit.GetComponent<BurningGround>()?.Extinguish();
                }
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy"))
            {
                EnemyBase enemy = collision.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeWaterDamage(damagePerSecond * Time.deltaTime);
                }
            }
            if (collision.CompareTag("Hazard") || collision.GetComponent<BurningGround>() != null)
            {
                collision.GetComponent<BurningGround>()?.Extinguish();
            }
        }
    }
}
