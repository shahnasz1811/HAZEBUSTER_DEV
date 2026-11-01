using UnityEngine;
using UnityEngine.UI;
using HazeBuster.Upgrades;

namespace HazeBuster.UI
{
    /// <summary>
    /// Individual card UI in the 3-choice level-up selection screen.
    /// Displays the card's title, description, and optional icon.
    /// </summary>
    public class UpgradeCardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Text titleText;
        public Text descriptionText;
        public Image iconImage;
        public Button selectButton;

        private UpgradeData assignedUpgrade;

        private void Awake()
        {
            if (selectButton == null)
            {
                selectButton = GetComponent<Button>();
            }
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnCardClicked);
            }
        }

        public void SetupCard(UpgradeData upgrade)
        {
            assignedUpgrade = upgrade;

            if (titleText != null)
            {
                titleText.text = upgrade.title;
            }
            if (descriptionText != null)
            {
                descriptionText.text = upgrade.description;
            }
            if (iconImage != null)
            {
                if (upgrade.icon != null)
                {
                    iconImage.sprite = upgrade.icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }
        }

        private void OnCardClicked()
        {
            if (assignedUpgrade != null)
            {
                UpgradeManager.Instance?.ApplySelectedUpgrade(assignedUpgrade);
            }
        }
    }
}
