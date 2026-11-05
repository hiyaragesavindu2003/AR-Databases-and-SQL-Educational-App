using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSQLExplorer.SQLJoin
{
    // Arranges existing controls only; callbacks and playback remain in SQLJoinController.
    public sealed class SQLJoinMobileLayout : MonoBehaviour
    {
        private RectTransform safeRoot;
        private CanvasScaler scaler;
        private RectTransform topPanel;
        private RectTransform bottomPanel;
        private Button[] joins;
        private Button[] actions;
        private TextMeshProUGUI status;
        private TextMeshProUGUI query;
        private TextMeshProUGUI legend;
        private TextMeshProUGUI explanation;
        private int screenWidth;
        private int screenHeight;
        private Rect safeArea;
        private RectTransform queryPanel;
        private Material uiTextMaterial;

        public void Configure(CanvasScaler canvasScaler, RectTransform controls, Button[] joinButtons,
            Button next, Button showAll, Button reset, TextMeshProUGUI statusText,
            TextMeshProUGUI queryText, TextMeshProUGUI legendText,
            RectTransform explanationPanel, TextMeshProUGUI explanationText)
        {
            scaler = canvasScaler;
            topPanel = controls;
            bottomPanel = explanationPanel;
            joins = joinButtons;
            actions = new[] { next, showAll, reset };
            status = statusText;
            query = queryText;
            legend = legendText;
            explanation = explanationText;
            safeRoot = new GameObject("Mobile Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(transform, false);
            topPanel.SetParent(safeRoot, false);
            foreach (var button in joins) button.transform.SetParent(safeRoot, false);
            foreach (var button in actions) button.transform.SetParent(safeRoot, false);
            status.transform.SetParent(safeRoot, false);
            query.transform.SetParent(safeRoot, false);
            bottomPanel.SetParent(safeRoot, false);
            legend.transform.SetParent(bottomPanel, false);
            // The shared font has a dark face tint for table labels. Never edit that asset.
            uiTextMaterial = new Material(query.fontSharedMaterial);
            uiTextMaterial.SetColor("_FaceColor", Color.white);
            foreach (var button in joins) ReadableText(button.GetComponentInChildren<TextMeshProUGUI>());
            foreach (var button in actions) ReadableText(button.GetComponentInChildren<TextMeshProUGUI>());
            ReadableText(status);
            ReadableText(query);
            ReadableText(legend);
            ReadableText(explanation);
            topPanel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.075f, 1);
            bottomPanel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.075f, 1);
            queryPanel = new GameObject("SQL Query Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            queryPanel.SetParent(safeRoot, false);
            queryPanel.SetSiblingIndex(1); // Above the top background, behind the controls.
            queryPanel.GetComponent<Image>().color = new Color(0.07f, 0.12f, 0.19f, 1);
            query.transform.SetParent(queryPanel, false);
            Refresh();
        }

        private void LateUpdate()
        {
            if (safeRoot != null && (screenWidth != Screen.width || screenHeight != Screen.height || safeArea != Screen.safeArea))
                Refresh();
        }

        private void Refresh()
        {
            screenWidth = Mathf.Max(1, Screen.width);
            screenHeight = Mathf.Max(1, Screen.height);
            safeArea = Screen.safeArea;
            Rect area = safeArea.width > 0 && safeArea.height > 0
                ? safeArea : new Rect(0, 0, screenWidth, screenHeight);
            safeRoot.anchorMin = new Vector2(area.xMin / screenWidth, area.yMin / screenHeight);
            safeRoot.anchorMax = new Vector2(area.xMax / screenWidth, area.yMax / screenHeight);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            bool portrait = area.height >= area.width;
            scaler.referenceResolution = portrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
            int columns = portrait ? 2 : 4;
            float buttonHeight = portrait ? 144 : 64;
            const float margin = 16;
            const float gap = 12;
            for (int i = 0; i < joins.Length; i++)
            {
                Slot(joins[i].GetComponent<RectTransform>(), i % columns, columns,
                    margin + (i / columns) * (buttonHeight + gap), buttonHeight);
                ButtonText(joins[i], portrait ? 38 : 28);
            }
            float actionTop = margin + (portrait ? 2 : 1) * (buttonHeight + gap);
            for (int i = 0; i < actions.Length; i++)
            {
                Slot(actions[i].GetComponent<RectTransform>(), i, 3, actionTop, buttonHeight);
                ButtonText(actions[i], portrait ? 38 : 28);
            }
            float statusTop = actionTop + buttonHeight + gap;
            float statusHeight = portrait ? 72 : 36;
            float queryTop = statusTop + statusHeight + gap;
            float queryHeight = portrait ? 236 : 156;
            Slot(status.rectTransform, 0, 1, statusTop, statusHeight);
            Slot(queryPanel, 0, 1, queryTop, queryHeight);
            query.rectTransform.anchorMin = Vector2.zero;
            query.rectTransform.anchorMax = Vector2.one;
            query.rectTransform.offsetMin = new Vector2(20, 12);
            query.rectTransform.offsetMax = new Vector2(-20, -12);
            query.alignment = TextAlignmentOptions.TopLeft;
            SetText(status, portrait ? 36 : 26, portrait ? 30 : 22);
            SetText(query, portrait ? 34 : 25, portrait ? 28 : 21);
            StretchPanel(topPanel, true, queryTop + queryHeight + margin);
            StretchPanel(bottomPanel, false, portrait ? 208 : 128);
            Slot(legend.rectTransform, 0, 1, 8, portrait ? 38 : 30);
            SetText(legend, portrait ? 27 : 20, portrait ? 24 : 18);
            explanation.rectTransform.anchorMin = Vector2.zero;
            explanation.rectTransform.anchorMax = Vector2.one;
            explanation.rectTransform.offsetMin = new Vector2(16, 12);
            explanation.rectTransform.offsetMax = new Vector2(-16, portrait ? -50 : -38);
            SetText(explanation, portrait ? 38 : 26, portrait ? 30 : 22);
        }

        private void ReadableText(TextMeshProUGUI text)
        {
            text.fontSharedMaterial = uiTextMaterial;
            text.color = Color.white;
        }

        private void OnDestroy()
        {
            if (uiTextMaterial != null) Destroy(uiTextMaterial);
        }

        private static void Slot(RectTransform rect, int column, int columns, float top, float height)
        {
            rect.anchorMin = new Vector2((float)column / columns, 1);
            rect.anchorMax = new Vector2((float)(column + 1) / columns, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(column == 0 ? 16 : 6, -top - height);
            rect.offsetMax = new Vector2(column == columns - 1 ? -16 : -6, -top);
        }

        private static void StretchPanel(RectTransform rect, bool top, float height)
        {
            rect.anchorMin = new Vector2(0, top ? 1 : 0);
            rect.anchorMax = new Vector2(1, top ? 1 : 0);
            rect.pivot = new Vector2(0.5f, top ? 1 : 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, height);
        }

        private static void ButtonText(Button button, float size)
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            text.rectTransform.offsetMin = new Vector2(12, 8);
            text.rectTransform.offsetMax = new Vector2(-12, -8);
            SetText(text, size, size - 6);
        }

        private static void SetText(TextMeshProUGUI text, float maximum, float minimum)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = maximum;
            text.fontSizeMin = minimum;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
        }
    }
}
