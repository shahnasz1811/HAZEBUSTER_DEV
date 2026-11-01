using System;
using System.Collections;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.MetaProgression;

namespace HazeBuster.Player
{
    /// <summary>
    /// Manages the Scientist Engineer's health, damage invulnerability, and suit integrity.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public float baseMaxHealth = 100f;
        public float maxHealth;
        public float currentHealth;
        public bool isInvulnerable = false;
        public float hurtInvulnerabilityDuration = 1.0f;

        [Header("Visual Feedback")]
        public SpriteRenderer characterRenderer;
        public Color hurtColor = new Color(1f, 0.4f, 0.4f, 0.7f);
        private Color normalColor = Color.white;

        public event Action<float, float> OnHealthChanged; // (current, max)

        private void Awake()
        {
            if (characterRenderer == null)
            {
                characterRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (characterRenderer != null)
            {
                normalColor = characterRenderer.color;
            }
        }

        private void Start()
        {
            ApplyMetaBonuses();
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyMetaBonuses()
        {
            float bonusHealth = MetaProgressionManager.Instance != null
                ? MetaProgressionManager.Instance.GetMaxHealthBonus()
                : 0f;
            maxHealth = baseMaxHealth + bonusHealth;
        }

        public void TakeDamage(float damageAmount)
        {
            if (isInvulnerable || currentHealth <= 0) return;

            currentHealth -= damageAmount;
            currentHealth = Mathf.Max(0f, currentHealth);

            SoundManager.Instance?.PlayHitSound();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(HurtInvulnerabilityRoutine());
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private IEnumerator HurtInvulnerabilityRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;
            float flashInterval = 0.1f;

            while (elapsed < hurtInvulnerabilityDuration)
            {
                if (characterRenderer != null)
                {
                    characterRenderer.color = hurtColor;
                    yield return new WaitForSeconds(flashInterval);
                    characterRenderer.color = normalColor;
                    yield return new WaitForSeconds(flashInterval);
                }
                else
                {
                    yield return new WaitForSeconds(flashInterval * 2);
                }
                elapsed += flashInterval * 2;
            }

            if (characterRenderer != null)
            {
                characterRenderer.color = normalColor;
            }
            isInvulnerable = false;
        }

        private void Die()
        {
            Debug.Log("[PlayerHealth] Scientist Engineer fell in the haze.");
            GameManager.Instance?.TriggerGameOver();
        }
    }
}
