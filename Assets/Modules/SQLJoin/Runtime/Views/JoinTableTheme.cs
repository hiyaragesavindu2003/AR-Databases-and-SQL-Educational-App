using UnityEngine;

namespace ARSQLExplorer.SQLJoin
{
    [CreateAssetMenu(menuName = "SQL JOIN/Table Color Theme")]
    public sealed class JoinTableTheme : ScriptableObject
    {
        public Color studentsTitle;
        public Color marksTitle;
        public Color resultTitle;
        public Color titleText;
        public Color header;
        public Color headerText;
        public Color row;
        public Color alternateRow;
        public Color cellText;
        public Color primaryKey;
        public Color primaryKeyText;
        public Color nullCell;
    }
}
