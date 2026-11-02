using UnityEngine;
using HazeBuster.MetaProgression;

namespace HazeBuster.Player
{
    /// <summary>
    /// Controls the dynamic vision circle cutting through the haze.
    /// Supports both Unity URP 2D Light2D (via reflection) and standard SpriteMask / Scaled circles,
    /// so it works 100% cleanly without compilation errors regardless of your project template!
    /// </summary>
    public class PlayerVision : MonoBehaviour
    {
        [Header("Vision Circle Settings")]
        public float baseVisionRadius = 6.0f;
        public float currentVisionRadius;
        public float innerRadiusRatio = 0.5f;

        [Header("2D Light Component (Drag Light2D here if using URP)")]
        [Tooltip("If you have a 2D Light attached, drag it here. It will automatically adjust.")]
        public Component playerLight;

        [Header("Sprite Mask (For standard 2D without URP lighting)")]
        public SpriteMask optionalSpriteMask;

        [Header("Vision Circle Visual (Optional scale transform)")]
        public Transform visionVisualTransform;

        private System.Reflection.PropertyInfo outerRadiusProp;
        private System.Reflection.PropertyInfo innerRadiusProp;
        private bool hasLookedForLightProps = false;

        private void Awake()
        {
            if (playerLight == null)
            {
                // Auto-detect any 2D light attached to player or children
                Component[] comps = GetComponentsInChildren<Component>();
                foreach (var c in comps)
                {
                    if (c != null && c.GetType().Name == "Light2D")
                    {
                        playerLight = c;
                        break;
                    }
                }
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
            // 1. Update URP 2D Light via reflection (safe without hard compile dependency)
            if (playerLight != null)
            {
                if (!hasLookedForLightProps)
                {
                    var type = playerLight.GetType();
                    outerRadiusProp = type.GetProperty("pointLightOuterRadius");
                    innerRadiusProp = type.GetProperty("pointLightInnerRadius");
                    hasLookedForLightProps = true;
                }

                if (outerRadiusProp != null)
                {
                    outerRadiusProp.SetValue(playerLight, currentVisionRadius);
                }
                if (innerRadiusProp != null)
                {
                    innerRadiusProp.SetValue(playerLight, currentVisionRadius * innerRadiusRatio);
                }
            }

            // 2. Update SpriteMask if used
            if (optionalSpriteMask != null)
            {
                optionalSpriteMask.transform.localScale = Vector3.one * (currentVisionRadius * 2f);
            }

            // 3. Update any visual circle transform if assigned
            if (visionVisualTransform != null)
            {
                visionVisualTransform.localScale = Vector3.one * (currentVisionRadius * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0.5f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, currentVisionRadius);
        }
    }
}
