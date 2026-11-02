using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using HazeBuster.UI;

namespace HazeBuster.Editor
{
    /// <summary>
    /// 1-Click Game UI & Level-Up Menu Generator for Art Students!
    /// Generates the full Canvas, HUD (Health, EXP, Clock, Dash), and the 3-Card Level-Up Popup
    /// with zero coding required!
    /// Access via: HazeBuster -> Create Complete Game UI & Level-Up Menu
    ///
    /// Safe to run again: it reuses what already exists and repairs it. Every UI object it touches
    /// is guaranteed to have a RectTransform (earlier versions made plain Transforms, which crashed
    /// the tool halfway and left the Level-Up menu unbuilt and unwired).
    /// </summary>
    public static class UISetupTool
    {
        [MenuItem("HazeBuster/Create Complete Game UI & Level-Up Menu")]
        public static void CreateFullUI()
        {
            // 1. Check or Create EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 2. Check or Create Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("HUD_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            // 3. Find or Create UIManager GameObject
            GameObject uiManagerObj = GameObject.Find("_UIManager");
            if (uiManagerObj == null)
            {
                uiManagerObj = new GameObject("_UIManager");
            }
            UIManager uiManager = uiManagerObj.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = uiManagerObj.AddComponent<UIManager>();
            }

            // --- BUILD HUD ---
            GameObject hudPanel = FindOrCreateChild(canvasObj, "HUD_Panel");
            SetStretchFull(hudPanel.GetComponent<RectTransform>());

            // A. Top-Left Health Bar
            GameObject hpObj = FindOrCreateChild(hudPanel, "HealthBar");
            Slider hpSlider = SetupSlider(hpObj, new Vector2(-750, 480), new Vector2(300, 28), new Color(0.85f, 0.2f, 0.2f));
            uiManager.healthSlider = hpSlider;

            GameObject hpTextObj = FindOrCreateChild(hpObj, "HP_Text");
            Text hpText = SetupText(hpTextObj, "100 / 100", 18, Color.white, TextAnchor.MiddleCenter, new Vector2(300, 28));
            SetStretchFull(hpTextObj.GetComponent<RectTransform>()); // text sits centred on the bar
            uiManager.healthText = hpText;

            // B. Top-Center EXP Bar
            GameObject expObj = FindOrCreateChild(hudPanel, "EXP_Bar");
            Slider expSlider = SetupSlider(expObj, new Vector2(0, 490), new Vector2(500, 20), new Color(0.2f, 0.85f, 1f));
            uiManager.expSlider = expSlider;

            GameObject lvlTextObj = FindOrCreateChild(hudPanel, "Level_Text");
            Text lvlText = SetupText(lvlTextObj, "Lv. 1", 22, Color.cyan, TextAnchor.MiddleCenter, new Vector2(200, 30));
            lvlText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 455);
            uiManager.levelText = lvlText;

            // C. Survival Timer Clock
            GameObject timerObj = FindOrCreateChild(hudPanel, "SurvivalTimer_Text");
            Text timerText = SetupText(timerObj, "00:00", 32, Color.white, TextAnchor.MiddleCenter, new Vector2(200, 40));
            timerText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 415);
            uiManager.timerText = timerText;

            // D. Dash Indicator
            GameObject dashObj = FindOrCreateChild(hudPanel, "Dash_Text");
            Text dashText = SetupText(dashObj, "Dash: 1 / 1", 22, new Color(0.4f, 1f, 0.9f), TextAnchor.MiddleLeft, new Vector2(300, 30));
            dashText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-790, -480);
            uiManager.dashText = dashText;

            // E. Dew Collected
            GameObject dewObj = FindOrCreateChild(hudPanel, "Dew_Text");
            Text dewText = SetupText(dewObj, "Dew: 0", 22, new Color(0.3f, 1f, 0.6f), TextAnchor.MiddleRight, new Vector2(300, 30));
            dewText.GetComponent<RectTransform>().anchoredPosition = new Vector2(790, 480);
            uiManager.dewCountText = dewText;

            // --- BUILD LEVEL-UP MODAL POPUP ---
            GameObject lvlUpPanel = FindOrCreateChild(canvasObj, "LevelUp_Modal");
            SetStretchFull(lvlUpPanel.GetComponent<RectTransform>());
            lvlUpPanel.transform.SetAsLastSibling(); // must draw on top of the HUD

            Image bgOverlay = lvlUpPanel.GetComponent<Image>();
            if (bgOverlay == null) bgOverlay = lvlUpPanel.AddComponent<Image>();
            bgOverlay.color = new Color(0.08f, 0.08f, 0.1f, 0.85f); // Dark translucent backdrop

            // Modal Header
            GameObject headerObj = FindOrCreateChild(lvlUpPanel, "Header_Text");
            Text headerText = SetupText(headerObj, "HAZE CLEARED — FIELD UPGRADE DISPATCH", 34, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter, new Vector2(1100, 60));
            headerText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 280);

            // Cards Horizontal Container
            GameObject cardContainer = FindOrCreateChild(lvlUpPanel, "Cards_Container");
            RectTransform cardContainerRect = cardContainer.GetComponent<RectTransform>();
            cardContainerRect.sizeDelta = new Vector2(1100, 450);
            cardContainerRect.anchoredPosition = new Vector2(0, -20);

            HorizontalLayoutGroup hlg = cardContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = cardContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 35;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Create 3 Upgrade Cards
            UpgradeCardUI[] cards = new UpgradeCardUI[3];
            for (int i = 0; i < 3; i++)
            {
                cards[i] = CreateCard(cardContainer, i + 1);
            }

            uiManager.levelUpPanel = lvlUpPanel;
            uiManager.upgradeCards = cards;

            // Start with LevelUp Panel hidden
            lvlUpPanel.SetActive(false);

            // The fields above were changed on an existing component, so tell Unity the scene needs saving.
            EditorUtility.SetDirty(uiManager);
            EditorSceneManager.MarkSceneDirty(uiManagerObj.scene);

            Selection.activeGameObject = uiManagerObj;
            EditorUtility.DisplayDialog("HAZEBUSTER", "Success! Complete HUD and 3-Card Level-Up Menu built and wired automatically!\n\nSave the scene now (Ctrl+S).", "Awesome!");
            Debug.Log("<color=green>[HAZEBUSTER] HUD & Level-Up Menu created successfully in Canvas! Save the scene (Ctrl+S).</color>");
        }

        private static UpgradeCardUI CreateCard(GameObject parent, int index)
        {
            GameObject cardObj = FindOrCreateChild(parent, $"UpgradeCard_{index}");
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(300, 420);

            Image cardBg = cardObj.GetComponent<Image>();
            if (cardBg == null) cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.15f, 0.22f, 0.25f, 0.95f); // Deep tech teal

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.AddComponent<Button>();
            btn.targetGraphic = cardBg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.25f, 0.45f, 0.5f, 1f);
            cb.pressedColor = new Color(0.1f, 0.35f, 0.4f, 1f);
            btn.colors = cb;

            UpgradeCardUI cardUI = cardObj.GetComponent<UpgradeCardUI>();
            if (cardUI == null) cardUI = cardObj.AddComponent<UpgradeCardUI>();
            cardUI.selectButton = btn;

            // Card Title
            GameObject titleObj = FindOrCreateChild(cardObj, "Title_Text");
            Text title = SetupText(titleObj, "Upgrade Title", 22, new Color(1f, 0.9f, 0.3f), TextAnchor.MiddleCenter, new Vector2(270, 60));
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
            cardUI.titleText = title;

            // Card Description
            GameObject descObj = FindOrCreateChild(cardObj, "Desc_Text");
            Text desc = SetupText(descObj, "Increases water pressure and attack power.", 18, Color.white, TextAnchor.UpperCenter, new Vector2(260, 160));
            desc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
            cardUI.descriptionText = desc;

            // Select Prompt
            GameObject promptObj = FindOrCreateChild(cardObj, "SelectPrompt_Text");
            Text prompt = SetupText(promptObj, "[ CLICK TO SELECT ]", 15, new Color(0.3f, 1f, 0.6f), TextAnchor.MiddleCenter, new Vector2(260, 30));
            prompt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -165);

            return cardUI;
        }

        /// <summary>
        /// Finds a child by name or creates it. The result ALWAYS has a RectTransform, even if an
        /// older run of this tool left a plain Transform behind (Unity converts it in place and
        /// keeps its children).
        /// </summary>
        private static GameObject FindOrCreateChild(GameObject parent, string name)
        {
            Transform t = parent.transform.Find(name);
            GameObject obj;
            if (t != null)
            {
                obj = t.gameObject;
            }
            else
            {
                obj = new GameObject(name, typeof(RectTransform));
                obj.transform.SetParent(parent.transform, false);
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.localScale = Vector3.one; // repairs objects that were scaled by accident
            return obj;
        }

        private static Slider SetupSlider(GameObject obj, Vector2 pos, Vector2 size, Color fillColor)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            // Background
            Image bg = obj.GetComponent<Image>();
            if (bg == null) bg = obj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.15f, 0.8f);

            // Fill Area
            GameObject fillArea = FindOrCreateChild(obj, "Fill Area");
            SetStretchFull(fillArea.GetComponent<RectTransform>());

            GameObject fill = FindOrCreateChild(fillArea, "Fill");
            SetStretchFull(fill.GetComponent<RectTransform>());
            Image fillImg = fill.GetComponent<Image>();
            if (fillImg == null) fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            Slider slider = obj.GetComponent<Slider>();
            if (slider == null) slider = obj.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.interactable = false; // bars are display only
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static Text SetupText(GameObject obj, string content, int fontSize, Color col, TextAnchor alignment, Vector2 size)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Text txt = obj.GetComponent<Text>();
            if (txt == null) txt = obj.AddComponent<Text>();
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = col;
            txt.alignment = alignment;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false; // clicks go through the text to the card button behind it
            return txt;
        }

        private static void SetStretchFull(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
