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
        [SerializeField] private Material connectionMaterialTemplate;
        [SerializeField] private bool useMobileLayout;
        [SerializeField] private JoinTableTheme arTableTheme;
        [SerializeField, Min(0.1f)] private float connectionDrawSeconds = 1.2f;
        [SerializeField, Min(0f)] private float resultHoldSeconds = 0.8f;
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
        private JoinRowView connectionStudent;
        private JoinRowView connectionMark;
        private float connectionProgress;
        private Transform connectionsRoot;
        private bool uiInitialized;
        private Button resetButton;
        public bool IsBound => studentsTable != null && marksTable != null && resultTable != null && playback != null;
        private static readonly Color Examined = new Color(0.25f, 0.55f, 1f);
        private static readonly Color Matched = new Color(0.3f, 0.85f, 0.4f);
        private static readonly Color Unmatched = new Color(1f, 0.65f, 0.2f);
        private readonly Button[] buttons = new Button[4];
        private static readonly string[] Names = { "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL OUTER JOIN" };

        private void Start()
        {
            if (!InitializeUI()) return;
            if (IsBound) return;
            if (studentsTable != null && marksTable != null && resultTable != null)
                BindViews(studentsTable, marksTable, resultTable, resultTable.transform.parent);
            else UpdateStatus();
        }

        private bool InitializeUI()
        {
            if (uiInitialized) return true;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null || GetComponent<Canvas>() == null || GetComponent<CanvasScaler>() == null)
            {
                Debug.LogError("JOIN UI requires a Canvas, CanvasScaler and TMP font.", this);
                return false;
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
            resetButton = ActionButton("Reset", 1180, ResetPlayback);
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
            if (useMobileLayout)
            {
                var mobileLayout = gameObject.AddComponent<SQLJoinMobileLayout>();
                mobileLayout.Configure(scaler, panel.GetComponent<RectTransform>(), buttons,
                    nextButton, showAllButton, resetButton, status, query, legend,
                    panelRect, explanation);
            }
            uiInitialized = true;
            UpdateStatus();
            return true;
        }

        public bool BindVisualization(SQLJoinVisualization visualization)
        {
            if (visualization != null && arTableTheme != null && !visualization.SetTheme(arTableTheme))
            {
                Debug.LogError("Apply the AR table theme before visualization initialization.", this);
                return false;
            }
            if (visualization == null || !InitializeUI() || !visualization.Initialize()) return false;
            return BindViews(visualization.Students, visualization.Marks, visualization.Result, visualization.Connections);
        }

        private bool BindViews(JoinTableView students, JoinTableView marks, JoinTableView result, Transform connections)
        {
            if (students == null || marks == null || result == null || connections == null ||
                students == marks || students == result || marks == result) return false;
            if (!students.Initialize() || !marks.Initialize() || !result.Initialize()) return false;
            UnbindVisualization();
            studentsTable = students;
            marksTable = marks;
            resultTable = result;
            connectionsRoot = connections;
            playback = new JoinPlayback(JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, selectedType));
            SelectJoin(selectedType);
            return true;
        }

        public void UnbindVisualization()
        {
            CancelStep();
            if (resultTable != null && resultTable.IsInitialized) resultTable.ShowResults(new JoinResult[0]);
            if (connection != null)
            {
                connection.gameObject.SetActive(false);
                Destroy(connection.gameObject);
            }
            if (connectionMaterial != null) Destroy(connectionMaterial);
            connection = null;
            connectionMaterial = null;
            connectionsRoot = null;
            studentsTable = marksTable = resultTable = null;
            playback = null;
            if (uiInitialized) UpdateStatus();
        }

        public void SelectJoin(JoinType type)
        {
            if (!uiInitialized || !IsBound) return;
            CancelStep();
            selectedType = type;
            playback = new JoinPlayback(JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, type));
            string name = Names[(int)type];
            query.text = useMobileLayout
                ? "SELECT s.Student_ID, s.Name,\n"
                    + "  m.Student_ID, m.Subject, m.Score\n"
                    + "FROM Students AS s\n"
                    + name + " Marks AS m\n"
                    + "  ON s.Student_ID = m.Student_ID;"
                : "SELECT s.Student_ID, s.Name, m.Student_ID, m.Subject, m.Score\n"
                    + "FROM Students AS s " + name + " Marks AS m ON s.Student_ID = m.Student_ID;";
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].GetComponent<Image>().color = i == (int)type
                    ? new Color(0.08f, 0.48f, 0.58f) : new Color(0.16f, 0.23f, 0.32f);
            ResetPlayback();
        }

        public void NextStep()
        {
            if (!IsBound || animating || !playback.CanAdvance) return;
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
                connectionStudent = student;
                connectionMark = mark;
                connectionProgress = 0;
                UpdateConnection();
                connection.enabled = true;
                explanation.text = "Student_ID " + id + " matches. Connecting " + result.Student.Name
                    + " to Mark_ID " + result.Mark.MarkId + " (" + result.Mark.Subject + ").";
                float elapsed = 0;
                float duration = Mathf.Max(0.1f, connectionDrawSeconds);
                while (elapsed < duration)
                {
                    yield return null;
                    elapsed += Time.unscaledDeltaTime;
                    connectionProgress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                }
                connectionProgress = 1;
                UpdateConnection();
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
            // Keep the completed relationship visible before enabling another step.
            yield return new WaitForSecondsRealtime(Mathf.Max(0, resultHoldSeconds));
            animating = false;
            stepAnimation = null;
            UpdateStatus();
        }

        public void ShowAll()
        {
            if (!IsBound) return;
            CancelStep();
            playback.ShowAll();
            resultTable.ShowResults(playback.Results);
            explanation.text = "Showing all " + playback.Results.Count + " result rows for " + Names[(int)selectedType]
                + ". Each matching pair is retained; gray NULL cells indicate a missing side. Reset to replay.";
            UpdateStatus();
        }

        public void ResetPlayback()
        {
            if (!IsBound) return;
            CancelStep();
            playback.Reset();
            resultTable.ShowResults(new JoinResult[0]);
            explanation.text = Names[(int)selectedType] + " ready. Next Step examines one result at a time; Show All displays the complete result.";
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (!uiInitialized) return;
            foreach (var button in buttons) button.interactable = IsBound;
            resetButton.interactable = IsBound;
            if (!IsBound)
            {
                status.text = "Waiting for SQL JOIN visualization.";
                query.text = "";
                explanation.text = "Place or bind a visualization to begin.";
                nextButton.interactable = showAllButton.interactable = false;
                return;
            }
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
            connectionStudent = null;
            connectionMark = null;
            connectionProgress = 0;
        }

        private void LateUpdate()
        {
            if (connection != null && connection.enabled) UpdateConnection();
        }

        private void UpdateConnection()
        {
            if (connectionStudent == null || connectionMark == null) return;
            Vector3 from = connectionStudent.KeyConnectionPoint(0);
            Vector3 to = connectionMark.KeyConnectionPoint(1);
            connection.SetPosition(0, from);
            connection.SetPosition(1, Vector3.Lerp(from, to, connectionProgress));
            // LineRenderer width is world-space; scale it with the visualization.
            var scale = connection.transform.lossyScale;
            connection.widthMultiplier = 0.035f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
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
            if (connectionMaterialTemplate != null)
            {
                if (connectionMaterialTemplate.shader == null || !connectionMaterialTemplate.shader.isSupported)
                {
                    Debug.LogError("JOIN connection material shader is missing or unsupported.", this);
                    return false;
                }
                connectionMaterial = new Material(connectionMaterialTemplate);
            }
            else
            {
                // Existing desktop scene compatibility; AR scene assigns the material explicitly.
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null || !shader.isSupported)
                {
                    Debug.LogError("JOIN connection unlit shader is missing or unsupported; assign a material.", this);
                    return false;
                }
                connectionMaterial = new Material(shader);
            }
            if (connectionMaterial.HasProperty("_BaseColor")) connectionMaterial.SetColor("_BaseColor", Matched);
            if (connectionMaterial.HasProperty("_Color")) connectionMaterial.SetColor("_Color", Matched);
            connection = new GameObject("Matching Student_ID Connection").AddComponent<LineRenderer>();
            connection.transform.SetParent(connectionsRoot, false);
            connection.sharedMaterial = connectionMaterial;
            connection.useWorldSpace = true;
            connection.positionCount = 2;
            connection.startWidth = connection.endWidth = 1f;
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
