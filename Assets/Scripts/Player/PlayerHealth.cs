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
        public float hurtInvulnerabilityDuration = 1.0f;

        // Dash protection and "just got hurt" protection are tracked separately, so one of them
        // ending never switches the other one off early.
        private bool hurtInvulnerable = false;
        private bool dashInvulnerable = false;

        /// <summary>True while the player cannot take damage (dashing, or recovering from a hit).</summary>
        public bool isInvulnerable => hurtInvulnerable || dashInvulnerable;

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

        /// <summary>
        /// Called by PlayerController at the start and end of a dash.
        /// </summary>
        public void SetDashInvulnerable(bool value)
        {
            dashInvulnerable = value;
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
            hurtInvulnerable = true;
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
            hurtInvulnerable = false;
        }

        private void Die()
        {
            Debug.Log("[PlayerHealth] Scientist Engineer fell in the haze.");
            GameManager.Instance?.TriggerGameOver();
        }
    }
}
