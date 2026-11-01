using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ARSQLExplorer.SQLJoin
{
    public sealed class JoinRowView : MonoBehaviour
    {
        public StudentRecord Student { get; private set; }
        public MarkRecord Mark { get; private set; }
        public bool IsHeader { get; private set; }

        public void Build(IReadOnlyList<string> values, IReadOnlyList<float> widths,
            int primaryKeyColumn, bool header, Material material, TMP_FontAsset font,
            StudentRecord student = null, MarkRecord mark = null)
        {
            Student = student;
            Mark = mark;
            IsHeader = header;
            float x = 0;
            for (int i = 0; i < values.Count; i++)
            {
                var cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cell.name = "Cell_" + i;
                cell.transform.SetParent(transform, false);
                cell.transform.localPosition = new Vector3(x + widths[i] / 2, 0, 0);
                cell.transform.localScale = new Vector3(widths[i] - 0.035f, 0.59f, 0.12f);
                var renderer = cell.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                var properties = new MaterialPropertyBlock();
                Color color = header ? new Color(0.08f, 0.24f, 0.36f) : new Color(0.83f, 0.9f, 0.94f);
                if (i == primaryKeyColumn)
                    color = header ? new Color(0.5f, 0.32f, 0.06f) : new Color(1f, 0.87f, 0.52f);
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(properties);

                var label = CreateLabel(transform, values[i], font, widths[i] - 0.1f, 0.53f,
                    header ? Color.white : new Color(0.03f, 0.08f, 0.12f));
                label.transform.localPosition = new Vector3(x + widths[i] / 2, 0, -0.075f);
                label.fontStyle = header ? FontStyles.Bold : FontStyles.Normal;
                label.fontSize = header ? 2.5f : 3f;
                x += widths[i];
            }
        }

        internal static TextMeshPro CreateLabel(Transform parent, string text,
            TMP_FontAsset font, float width, float height, Color color)
        {
            var label = new GameObject("Label", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(parent, false);
            label.font = font;
            label.text = text;
            label.fontSize = 3;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.rectTransform.sizeDelta = new Vector2(width, height);
            return label;
        }
    }
}
