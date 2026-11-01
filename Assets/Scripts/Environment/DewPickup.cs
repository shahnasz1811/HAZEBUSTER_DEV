using UnityEngine;
using HazeBuster.Player;

namespace HazeBuster.Environment
{
    /// <summary>
    /// Morning Dew / Moisture droplet collectible.
    /// Gained by extinguishing fire hazards. Attracted magnetically to the player.
    /// Feeds EXP for in-run leveling and converts to permanent Eco-Credits.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DewPickup : MonoBehaviour
    {
        [Header("Values")]
        public int expValue = 1;
        public float flySpeed = 8f;
        public float acceleration = 12f;

        private Transform playerTransform;
        private PlayerExperience playerExp;
        private bool isBeingAttracted = false;
        private float currentSpeed;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerExp = player.GetComponent<PlayerExperience>();
            }

            currentSpeed = flySpeed;
        }

        private void Update()
        {
            if (playerTransform == null) return;

            float distance = Vector2.Distance(transform.position, playerTransform.position);

            // Check magnet radius
            float magnetRadius = playerExp != null ? playerExp.currentMagnetRadius : 3f;
            if (distance <= magnetRadius)
            {
                isBeingAttracted = true;
            }

            if (isBeingAttracted)
            {
                currentSpeed += acceleration * Time.deltaTime;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTransform.position,
                    currentSpeed * Time.deltaTime
                );

                // Collect when close
                if (distance < 0.4f)
                {
                    Collect();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Collect();
            }
        }

        private void Collect()
        {
            if (playerExp != null)
            {
                playerExp.CollectDew(expValue);
            }
            Destroy(gameObject);
        }
    }
}
