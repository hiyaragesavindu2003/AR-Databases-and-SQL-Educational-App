using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSQLExplorer.SQLJoin
{
    public sealed class SQLJoinController : MonoBehaviour
    {
        [SerializeField] private JoinTableView resultTable;
        [SerializeField] private TMP_FontAsset font;
        private TextMeshProUGUI status;
        private TextMeshProUGUI query;
        private readonly Button[] buttons = new Button[4];
        private static readonly string[] Names = { "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL OUTER JOIN" };

        private void Start()
        {
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (resultTable == null || font == null)
            {
                Debug.LogError("Assign JOIN Result table and TMP font to SQLJoinController.", this);
                return;
            }
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("JOIN Controls", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            Place(panel.GetComponent<RectTransform>(), 960, -106, 1920, 212);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.075f, 0.98f);
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                var go = new GameObject(Names[i], typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                Place(go.GetComponent<RectTransform>(), 300 + i * 440, -38, 420, 58);
                var button = go.GetComponent<Button>();
                button.targetGraphic = go.GetComponent<Image>();
                var label = Label(go.transform, "Label", 28);
                label.text = Names[i];
                label.alignment = TextAlignmentOptions.Center;
                var rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                button.onClick.AddListener(() => SelectJoin((JoinType)index));
                buttons[i] = button;
            }
            status = Label(transform, "Selected JOIN and row count", 26);
            Place(status.rectTransform, 960, -91, 1760, 36);
            query = Label(transform, "SQL Query", 23);
            Place(query.rectTransform, 960, -156, 1760, 92);
            SelectJoin(JoinType.Inner);
        }

        public void SelectJoin(JoinType type)
        {
            var results = JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, type);
            resultTable.ShowResults(results);
            string name = Names[(int)type];
            status.text = name + "  |  Result rows: " + results.Count + "  |  Gray cells = NULL (missing side)";
            query.text = "SELECT s.Student_ID, s.Name, m.Student_ID, m.Subject, m.Score\n"
                + "FROM Students AS s " + name + " Marks AS m ON s.Student_ID = m.Student_ID;";
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].GetComponent<Image>().color = i == (int)type
                    ? new Color(0.08f, 0.48f, 0.58f) : new Color(0.16f, 0.23f, 0.32f);
        }

        private TextMeshProUGUI Label(Transform parent, string name, float size)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            label.font = font;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return label;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
