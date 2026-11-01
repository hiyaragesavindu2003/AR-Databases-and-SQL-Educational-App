using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace ARSQLExplorer.SQLJoin
{
    public sealed class JoinTableView : MonoBehaviour
    {
        public enum SourceTable { Students, Marks }
        [SerializeField] private SourceTable source;
        [SerializeField] private TMP_FontAsset font;
        private Material cellMaterial;
        private readonly List<JoinRowView> rows = new List<JoinRowView>();
        public IReadOnlyList<JoinRowView> Rows => rows.AsReadOnly();

        private void Start()
        {
            if (font == null) font = TMP_Settings.defaultFontAsset;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null || font == null)
            {
                Debug.LogError("JOIN table requires an unlit shader and a TMP font asset.", this);
                return;
            }
            cellMaterial = new Material(shader);
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

        private void AddRow(string[] values, float[] widths, int index, bool header,
            StudentRecord student = null, MarkRecord mark = null)
        {
            var row = new GameObject(header ? "Header" : "Row_" + index).AddComponent<JoinRowView>();
            row.transform.SetParent(transform, false);
            row.transform.localPosition = new Vector3(0, -index * 0.65f, 0);
            row.Build(values, widths, 0, header, cellMaterial, font, student, mark);
            if (!header) rows.Add(row);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private void OnDestroy()
        {
            if (cellMaterial != null) Destroy(cellMaterial);
        }
    }
}
