using UnityEngine;
using UnityEngine.Rendering.Universal;
using HazeBuster.MetaProgression;

namespace HazeBuster.Player
{
    /// <summary>
    /// Controls the dynamic vision circle cutting through the haze.
    /// Drives Unity 6 URP 2D Light2D (Point Light) and adjusts radius when upgraded.
    /// </summary>
    public class PlayerVision : MonoBehaviour
    {
        [Header("Vision Circle Settings")]
        public float baseVisionRadius = 6.0f;
        public float currentVisionRadius;
        public float innerRadiusRatio = 0.5f;

        [Header("Light Component")]
        public Light2D playerLight;

        [Header("Fallback Sprite Mask (if not using 2D Lighting)")]
        public SpriteMask optionalSpriteMask;

        private void Awake()
        {
            if (playerLight == null)
            {
                playerLight = GetComponentInChildren<Light2D>();
            }
        }

        private void Start()
        {
            ApplyMetaBonuses();
            UpdateVisionVisuals();
        }

        public void ApplyMetaBonuses()
        {
            float bonusVision = MetaProgressionManager.Instance != null
                ? MetaProgressionManager.Instance.GetVisionRadiusBonus()
                : 0f;
            currentVisionRadius = baseVisionRadius + bonusVision;
        }

        public void ExpandVision(float additionalRadius)
        {
            currentVisionRadius += additionalRadius;
            UpdateVisionVisuals();
        }

        public void UpdateVisionVisuals()
        {
            if (playerLight != null)
            {
                playerLight.pointLightOuterRadius = currentVisionRadius;
                playerLight.pointLightInnerRadius = currentVisionRadius * innerRadiusRatio;
            }

            if (optionalSpriteMask != null)
            {
                optionalSpriteMask.transform.localScale = Vector3.one * (currentVisionRadius * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0.5f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, currentVisionRadius);
        }
    }
}
