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
        private Material cellMaterial;
        private readonly List<JoinRowView> rows = new List<JoinRowView>();
        public IReadOnlyList<JoinRowView> Rows => rows.AsReadOnly();

        private void Start()
        {
            if (source == SourceTable.Result) return;
            if (!EnsureResources()) return;
            BuildSource();
        }

        private bool EnsureResources()
        {
            if (cellMaterial != null) return true;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null || font == null)
            {
                Debug.LogError("JOIN table requires an unlit shader and a TMP font asset.", this);
                return false;
            }
            cellMaterial = new Material(shader);
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
            row.Build(values, widths, primaryKey, header, cellMaterial, font, student, mark, nullCells);
            if (!header) rows.Add(row);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private void OnDestroy()
        {
            if (cellMaterial != null) Destroy(cellMaterial);
        }
    }
}
