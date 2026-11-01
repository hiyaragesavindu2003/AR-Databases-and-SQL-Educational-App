using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSQLExplorer.SQLJoin
{
    public sealed class SQLJoinController : MonoBehaviour
    {
        [SerializeField] private JoinTableView resultTable;
        [SerializeField] private JoinTableView studentsTable;
        [SerializeField] private JoinTableView marksTable;
        [SerializeField] private TMP_FontAsset font;
        private TextMeshProUGUI status;
        private TextMeshProUGUI query;
        private TextMeshProUGUI explanation;
        private Button nextButton;
        private Button showAllButton;
        private JoinPlayback playback;
        private JoinType selectedType;
        private Coroutine stepAnimation;
        private bool animating;
        private LineRenderer connection;
        private Material connectionMaterial;
        private static readonly Color Examined = new Color(0.25f, 0.55f, 1f);
        private static readonly Color Matched = new Color(0.3f, 0.85f, 0.4f);
        private static readonly Color Unmatched = new Color(1f, 0.65f, 0.2f);
        private readonly Button[] buttons = new Button[4];
        private static readonly string[] Names = { "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL OUTER JOIN" };

        private void Start()
        {
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (resultTable == null || studentsTable == null || marksTable == null || font == null)
            {
                Debug.LogError("Assign the three JOIN tables and TMP font to SQLJoinController.", this);
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
            nextButton = ActionButton("Next Step", 300, NextStep);
            showAllButton = ActionButton("Show All", 740, ShowAll);
            ActionButton("Reset", 1180, ResetPlayback);
            var legend = Label(transform, "Highlight legend", 20);
            legend.text = "<color=#408CFF>Blue: examining</color>  <color=#4DD966>Green: match</color>  <color=#FFA633>Amber: unmatched</color>";
            Place(legend.rectTransform, 1630, -92, 430, 46);
            status = Label(transform, "Selected JOIN and row count", 24);
            Place(status.rectTransform, 960, -130, 1760, 30);
            query = Label(transform, "SQL Query", 21);
            Place(query.rectTransform, 960, -177, 1760, 62);
            var explanationPanel = new GameObject("Step Explanation", typeof(RectTransform), typeof(Image));
            explanationPanel.transform.SetParent(transform, false);
            var panelRect = explanationPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1920, 64);
            explanationPanel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.075f, 0.98f);
            explanation = Label(explanationPanel.transform, "Explanation", 23);
            explanation.rectTransform.anchorMin = Vector2.zero;
            explanation.rectTransform.anchorMax = Vector2.one;
            explanation.rectTransform.offsetMin = new Vector2(40, 2);
            explanation.rectTransform.offsetMax = new Vector2(-40, -2);
            SelectJoin(JoinType.Inner);
        }

        public void SelectJoin(JoinType type)
        {
            CancelStep();
            selectedType = type;
            playback = new JoinPlayback(JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, type));
            string name = Names[(int)type];
            query.text = "SELECT s.Student_ID, s.Name, m.Student_ID, m.Subject, m.Score\n"
                + "FROM Students AS s " + name + " Marks AS m ON s.Student_ID = m.Student_ID;";
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].GetComponent<Image>().color = i == (int)type
                    ? new Color(0.08f, 0.48f, 0.58f) : new Color(0.16f, 0.23f, 0.32f);
            ResetPlayback();
        }

        public void NextStep()
        {
            if (playback == null || animating || !playback.CanAdvance) return;
            ClearHighlights();
            animating = true;
            UpdateStatus();
            stepAnimation = StartCoroutine(AnimateStep(playback.Results[playback.ShownCount]));
        }

        private IEnumerator AnimateStep(JoinResult result)
        {
            var student = FindSource(studentsTable.Rows, result.Student, null);
            var mark = FindSource(marksTable.Rows, null, result.Mark);
            if (student != null) student.Highlight(Examined);
            if (mark != null) mark.Highlight(Examined);
            int id = result.Student != null ? result.Student.StudentId : result.Mark.StudentId;
            explanation.text = "Examining Student_ID " + id + " for result row " + (playback.ShownCount + 1) + ".";
            yield return new WaitForSecondsRealtime(0.4f);

            bool match = result.Student != null && result.Mark != null;
            if (student != null) student.Highlight(match ? Matched : Unmatched);
            if (mark != null) mark.Highlight(match ? Matched : Unmatched);
            if (match && student != null && mark != null && EnsureConnection())
            {
                Vector3 from = student.KeyConnectionPoint(0);
                Vector3 to = mark.KeyConnectionPoint(1);
                connection.SetPosition(0, from);
                connection.SetPosition(1, from);
                connection.enabled = true;
                float elapsed = 0;
                while (elapsed < 0.25f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    connection.SetPosition(1, Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / 0.25f)));
                    yield return null;
                }
            }

            // Commit only after the animation; cancelled steps never advance the cursor.
            if (playback.TryAdvance(out var next)) resultTable.AppendResult(next);
            explanation.text = match
                ? "Student_ID " + id + " matches on both sides: " + result.Student.Name
                    + " + Mark_ID " + result.Mark.MarkId + " (" + result.Mark.Subject + "). One result row added."
                : result.Student != null
                    ? "Student_ID " + id + " (" + result.Student.Name + ") has no Marks match. "
                        + Names[(int)selectedType] + " keeps this student; Marks columns are NULL."
                    : "Marks.Student_ID " + id + " (Mark_ID " + result.Mark.MarkId + ") has no Students match. "
                        + Names[(int)selectedType] + " keeps this mark; Students columns are NULL.";
            animating = false;
            stepAnimation = null;
            UpdateStatus();
        }

        public void ShowAll()
        {
            if (playback == null) return;
            CancelStep();
            playback.ShowAll();
            resultTable.ShowResults(playback.Results);
            explanation.text = "Showing all " + playback.Results.Count + " result rows for " + Names[(int)selectedType]
                + ". Each matching pair is retained; gray NULL cells indicate a missing side. Reset to replay.";
            UpdateStatus();
        }

        public void ResetPlayback()
        {
            if (playback == null) return;
            CancelStep();
            playback.Reset();
            resultTable.ShowResults(new JoinResult[0]);
            explanation.text = Names[(int)selectedType] + " ready. Next Step examines one result at a time; Show All displays the complete result.";
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            status.text = Names[(int)selectedType] + "  |  Result rows: " + playback.ShownCount + " / " + playback.Results.Count
                + (animating ? "  |  Examining..." : playback.CanAdvance ? "  |  Ready for Next Step" : "  |  Complete");
            nextButton.interactable = !animating && playback.CanAdvance;
            showAllButton.interactable = animating || playback.CanAdvance;
        }

        private static JoinRowView FindSource(IReadOnlyList<JoinRowView> rows, StudentRecord student, MarkRecord mark)
        {
            foreach (var row in rows)
                if ((student != null && ReferenceEquals(row.Student, student)) ||
                    (mark != null && ReferenceEquals(row.Mark, mark))) return row;
            return null;
        }

        private void ClearHighlights()
        {
            if (studentsTable != null)
                foreach (var row in studentsTable.Rows) row.RestoreColors();
            if (marksTable != null)
                foreach (var row in marksTable.Rows) row.RestoreColors();
            if (connection != null) connection.enabled = false;
        }

        private void CancelStep()
        {
            if (stepAnimation != null) StopCoroutine(stepAnimation);
            stepAnimation = null;
            animating = false;
            ClearHighlights();
        }

        private bool EnsureConnection()
        {
            if (connection != null) return true;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return false;
            connectionMaterial = new Material(shader);
            if (connectionMaterial.HasProperty("_BaseColor")) connectionMaterial.SetColor("_BaseColor", Matched);
            if (connectionMaterial.HasProperty("_Color")) connectionMaterial.SetColor("_Color", Matched);
            connection = new GameObject("Matching Student_ID Connection").AddComponent<LineRenderer>();
            connection.transform.SetParent(resultTable.transform.parent, false);
            connection.sharedMaterial = connectionMaterial;
            connection.useWorldSpace = true;
            connection.positionCount = 2;
            connection.startWidth = connection.endWidth = 0.035f;
            connection.startColor = connection.endColor = Matched;
            connection.enabled = false;
            return true;
        }

        private Button ActionButton(string title, float x, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            Place(go.GetComponent<RectTransform>(), x, -92, 420, 42);
            go.GetComponent<Image>().color = new Color(0.16f, 0.23f, 0.32f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(action);
            var label = Label(go.transform, "Label", 25);
            label.text = title;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private void OnDisable()
        {
            CancelStep();
            if (playback != null && status != null) UpdateStatus();
        }

        private void OnDestroy()
        {
            if (connection != null) Destroy(connection.gameObject);
            if (connectionMaterial != null) Destroy(connectionMaterial);
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
