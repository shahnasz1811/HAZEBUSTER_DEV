using System.Collections;
using UnityEngine;
using HazeBuster.Core;
using HazeBuster.MetaProgression;

namespace HazeBuster.Player
{
    /// <summary>
    /// Controls the Scientist Engineer: WASD movement, mouse aiming, and high-impact dashing.
    /// Includes upgradeable Dash systems (Hydro-Dash water trail, Steam Burst, extra charges).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float baseMoveSpeed = 5f;
        public float currentMoveSpeed;
        private Vector2 moveInput;
        private Rigidbody2D rb;

        [Header("Mouse Aiming")]
        public Camera mainCamera;
        public Transform weaponMount; // Child object representing the water gun barrel / hand
        public SpriteRenderer characterRenderer;

        [Header("Dash Settings")]
        public float baseDashCooldown = 1.5f;
        public float currentDashCooldown;
        public float dashSpeed = 16f;
        public float dashDuration = 0.2f;
        public int maxDashCharges = 1;
        public int currentDashCharges;
        public float dashRechargeTimer;
        public bool isDashing { get; private set; }

        [Header("Dash Upgrades")]
        public bool hasHydroDash = false;
        public GameObject waterTrailPrefab;
        public float waterTrailInterval = 0.04f;

        public bool hasSteamBurst = false;
        public float steamBurstRadius = 2.5f;
        public float steamBurstDamage = 25f;

        private PlayerHealth playerHealth;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerHealth = GetComponent<PlayerHealth>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Start()
        {
            // Apply permanent meta-progression bonuses
            ApplyMetaBonuses();

            currentDashCharges = maxDashCharges;
            currentDashCooldown = baseDashCooldown;
        }

        public void ApplyMetaBonuses()
        {
            float speedBonus = MetaProgressionManager.Instance != null 
                ? MetaProgressionManager.Instance.GetMoveSpeedBonus() 
                : 0f;
            currentMoveSpeed = baseMoveSpeed + speedBonus;

            float cooldownReduction = MetaProgressionManager.Instance != null
                ? MetaProgressionManager.Instance.GetDashCooldownReduction()
                : 0f;
            currentDashCooldown = Mathf.Max(0.5f, baseDashCooldown - cooldownReduction);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
            {
                moveInput = Vector2.zero;
                return;
            }

            // 1. Read Movement Input
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            moveInput = moveInput.normalized;

            // 2. Aim towards Mouse
            HandleMouseAiming();

            // 3. Recharge Dash Charges
            HandleDashRecharge();

            // 4. Dash Input (Spacebar or Right Mouse Button)
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1)) && currentDashCharges > 0 && !isDashing)
            {
                StartCoroutine(PerformDash());
            }
        }

        private void FixedUpdate()
        {
            if (isDashing) return; // Velocity is handled by the dash coroutine
            rb.linearVelocity = moveInput * currentMoveSpeed;
        }

        private void HandleMouseAiming()
        {
            if (mainCamera == null) return;

            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Vector2 aimDirection = (mouseWorldPos - transform.position);

            // Flip character sprite based on aim side
            if (characterRenderer != null)
            {
                characterRenderer.flipX = aimDirection.x < 0;
            }

            // Aim the weapon mount towards the mouse cursor
            if (weaponMount != null)
            {
                float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                weaponMount.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void HandleDashRecharge()
        {
            if (currentDashCharges < maxDashCharges)
            {
                dashRechargeTimer += Time.deltaTime;
                if (dashRechargeTimer >= currentDashCooldown)
                {
                    currentDashCharges++;
                    dashRechargeTimer = 0f;
                }
            }
            else
            {
                dashRechargeTimer = 0f;
            }
        }

        private IEnumerator PerformDash()
        {
            isDashing = true;
            currentDashCharges--;
            SoundManager.Instance?.PlayDashSound();

            // Invulnerability during dash
            if (playerHealth != null)
            {
                playerHealth.isInvulnerable = true;
            }

            // Dash direction (if moving, dash in movement direction; otherwise towards mouse)
            Vector2 dashDir = moveInput;
            if (dashDir == Vector2.zero)
            {
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                dashDir = ((Vector2)(mouseWorldPos - transform.position)).normalized;
            }

            // Trigger Steam Burst start if unlocked
            if (hasSteamBurst)
            {
                TriggerSteamBurst();
            }

            float timer = 0f;
            float trailTimer = 0f;

            while (timer < dashDuration)
            {
                rb.linearVelocity = dashDir * dashSpeed;
                timer += Time.deltaTime;

                // Hydro-dash: drop water puddles along the trail
                if (hasHydroDash)
                {
                    trailTimer += Time.deltaTime;
                    if (trailTimer >= waterTrailInterval)
                    {
                        trailTimer = 0f;
                        SpawnWaterTrail();
                    }
                }

                yield return null;
            }

            rb.linearVelocity = Vector2.zero;
            isDashing = false;

            if (playerHealth != null)
            {
                playerHealth.isInvulnerable = false;
            }

            // Trigger Steam Burst end if unlocked
            if (hasSteamBurst)
            {
                TriggerSteamBurst();
            }
        }

        private void SpawnWaterTrail()
        {
            if (waterTrailPrefab != null)
            {
                Instantiate(waterTrailPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                // Fallback: programmatic water trail puddle
                GameObject puddle = new GameObject("WaterTrailPuddle");
                puddle.transform.position = transform.position;
                puddle.AddComponent<WaterTrail>();
            }
        }

        private void TriggerSteamBurst()
        {
            SoundManager.Instance?.PlayExplosionSound();

            // Knock back and damage nearby burning enemies
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, steamBurstRadius);
            foreach (var col in hits)
            {
                if (col.CompareTag("Enemy"))
                {
                    Enemies.EnemyBase enemy = col.GetComponent<Enemies.EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeWaterDamage(steamBurstDamage);
                        Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                        enemy.ApplyKnockback(knockbackDir, 8f);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, steamBurstRadius);
        }
    }
}
