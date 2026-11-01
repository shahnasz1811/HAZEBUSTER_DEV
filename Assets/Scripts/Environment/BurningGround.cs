using System.Collections;
using UnityEngine;
using HazeBuster.Player;

namespace HazeBuster.Environment
{
    /// <summary>
    /// Smoldering peat hazard on the forest floor.
    /// Damages the player if stepped on, and can be extinguished with water.
    /// </summary>
    public class BurningGround : MonoBehaviour
    {
        [Header("Hazard Stats")]
        public float damagePerSecond = 15f;
        public float lifetime = 12f;
        public bool isExtinguished = false;

        [Header("Visuals")]
        public SpriteRenderer spriteRenderer;
        public Color activeFireColor = new Color(1f, 0.35f, 0.1f, 0.8f);
        public Color extinguishedColor = new Color(0.2f, 0.2f, 0.25f, 0.4f);

        [Header("Drops")]
        public GameObject dewPrefab;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = activeFireColor;
            }
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (isExtinguished) return;

            if (collision.CompareTag("Player"))
            {
                PlayerHealth health = collision.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damagePerSecond * Time.deltaTime);
                }
            }
        }

        public void Extinguish()
        {
            if (isExtinguished) return;
            isExtinguished = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = extinguishedColor;
            }

            // Spawn a reward dew drop for clearing the hazard
            if (dewPrefab != null && Random.value < 0.4f)
            {
                Instantiate(dewPrefab, transform.position, Quaternion.identity);
            }

            // Fade out
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            float elapsed = 0f;
            Color c = spriteRenderer != null ? spriteRenderer.color : extinguishedColor;

            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, elapsed / 0.5f));
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
