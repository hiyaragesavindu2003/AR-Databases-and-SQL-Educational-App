using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace ARSQLExplorer.SQLJoin
{
    public sealed class JoinTableView : MonoBehaviour
    {
        public enum SourceTable { Students, Marks, Result }
        [SerializeField] private SourceTable source;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Material materialTemplate;
        private Material cellMaterial;
        private Material themedTextMaterial;
        private JoinTableTheme theme;
        public bool SetTheme(JoinTableTheme value)
        {
            if (IsInitialized && theme != value) return false;
            theme = value;
            return true;
        }
        public bool IsInitialized { get; private set; }
        private readonly List<JoinRowView> rows = new List<JoinRowView>();
        public IReadOnlyList<JoinRowView> Rows => rows.AsReadOnly();

        private void Start()
        {
            Initialize();
        }

        public bool Initialize()
        {
            if (IsInitialized) return true;
            if (!EnsureResources()) return false;
            if (source == SourceTable.Result) ShowResults(new JoinResult[0]);
            else BuildSource();
            IsInitialized = true;
            return true;
        }

        private bool EnsureResources()
        {
            if (cellMaterial != null) return true;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogError("JOIN table TMP font is missing: " + name, this);
                return false;
            }
            if (materialTemplate != null)
            {
                if (materialTemplate.shader == null || !materialTemplate.shader.isSupported)
                {
                    Debug.LogError("JOIN table material shader is missing or unsupported: " + name, this);
                    return false;
                }
                cellMaterial = new Material(materialTemplate);
            }
            else
            {
                // Compatibility for existing desktop scene views; AR prefab uses an explicit material.
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null || !shader.isSupported)
                {
                    Debug.LogError("JOIN table unlit shader is missing or unsupported; assign a material: " + name, this);
                    return false;
                }
                cellMaterial = new Material(shader);
            }
            if (theme != null)
            {
                themedTextMaterial = new Material(font.material);
                themedTextMaterial.SetColor("_FaceColor", Color.white);
            }
            return true;
        }

        private void BuildSource()
        {
            bool students = source == SourceTable.Students;
            float[] widths = students ? new[] { 1.6f, 2f } : new[] { 1.3f, 1.6f, 2.2f, 1.1f };
            string[] headers = students
                ? new[] { "Student_ID\n[PK]", "Name" }
                : new[] { "Mark_ID\n[PK]", "Student_ID", "Subject", "Score" };
            float width = 0;
            foreach (float w in widths) width += w;
            var title = JoinRowView.CreateLabel(transform, source.ToString(), font, width, 0.6f, Color.white);
            title.transform.localPosition = new Vector3(width / 2, 0.8f, -0.075f);
            title.fontSize = 4.5f;
            title.fontStyle = FontStyles.Bold;
            StyleTitle(title, width);
            AddRow(headers, widths, 0, true);
            if (students)
            {
                foreach (var student in JoinDemoData.Students)
                    AddRow(new[] { Number(student.StudentId), student.Name }, widths, rows.Count + 1, false, student);
            }
            else
            {
                foreach (var mark in JoinDemoData.Marks)
                    AddRow(new[] { Number(mark.MarkId), Number(mark.StudentId), mark.Subject, Number(mark.Score) },
                        widths, rows.Count + 1, false, null, mark);
            }
        }

        public void ShowResults(IReadOnlyList<JoinResult> results)
        {
            if (!EnsureResources()) return;
            IsInitialized = true;
            // Hide immediately; Destroy completes at the end of the frame.
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            rows.Clear();
            float[] widths = { 2.3f, 1.6f, 2.3f, 1.8f, 1f };
            var title = JoinRowView.CreateLabel(transform, "JOIN Result", font, 9, 0.6f, Color.white);
            title.transform.localPosition = new Vector3(4.5f, 0.8f, -0.075f);
            title.fontSize = 4.5f;
            title.fontStyle = FontStyles.Bold;
            StyleTitle(title, 9);
            AddRow(new[] { "Students.\nStudent_ID", "Name", "Marks.\nStudent_ID", "Subject", "Score" },
                widths, 0, true, primaryKey: -1);
            foreach (var result in results)
            {
                AppendResult(result);
            }
        }

        public void AppendResult(JoinResult result)
        {
            if (!EnsureResources()) return;
            float[] widths = { 2.3f, 1.6f, 2.3f, 1.8f, 1f };
            bool noStudent = result.Student == null;
            bool noMark = result.Mark == null;
            AddRow(new[] {
                noStudent ? "NULL" : Number(result.Student.StudentId),
                noStudent ? "NULL" : result.Student.Name,
                noMark ? "NULL" : Number(result.Mark.StudentId),
                noMark ? "NULL" : result.Mark.Subject,
                noMark ? "NULL" : Number(result.Mark.Score)
            }, widths, rows.Count + 1, false, result.Student, result.Mark, -1,
                new[] { noStudent, noStudent, noMark, noMark, noMark });
        }

        private void AddRow(string[] values, float[] widths, int index, bool header,
            StudentRecord student = null, MarkRecord mark = null, int primaryKey = 0, bool[] nullCells = null)
        {
            var row = new GameObject(header ? "Header" : "Row_" + index).AddComponent<JoinRowView>();
            row.transform.SetParent(transform, false);
            row.transform.localPosition = new Vector3(0, -index * 0.65f, 0);
            row.Build(values, widths, primaryKey, header, cellMaterial, font, student, mark, nullCells,
                theme, index, themedTextMaterial);
            if (!header) rows.Add(row);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private void StyleTitle(TextMeshPro title, float width)
        {
            if (theme == null) return;
            title.text = source == SourceTable.Students ? "STUDENTS" : source == SourceTable.Marks ? "MARKS" : "JOIN RESULT";
            title.fontSharedMaterial = themedTextMaterial;
            title.color = theme.titleText;
            // Decorative UI backing uses the existing title footprint; cell geometry is untouched.
            var backing = new GameObject("Title Bar", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.Image));
            backing.transform.SetParent(transform, false);
            backing.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = backing.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 0.6f);
            rect.localPosition = new Vector3(width / 2, 0.8f, 0);
            var image = backing.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            image.color = source == SourceTable.Students ? theme.studentsTitle
                : source == SourceTable.Marks ? theme.marksTitle : theme.resultTitle;
        }

        private void OnDestroy()
        {
            if (cellMaterial != null) Destroy(cellMaterial);
            if (themedTextMaterial != null) Destroy(themedTextMaterial);
        }
    }
}
