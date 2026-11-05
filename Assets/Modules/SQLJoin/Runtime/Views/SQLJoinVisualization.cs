using UnityEngine;

namespace ARSQLExplorer.SQLJoin
{
    public sealed class SQLJoinVisualization : MonoBehaviour
    {
        [SerializeField] private JoinTableView students;
        [SerializeField] private JoinTableView marks;
        [SerializeField] private JoinTableView result;
        [SerializeField] private Transform connections;

        public JoinTableView Students => students;
        public JoinTableView Marks => marks;
        public JoinTableView Result => result;
        public Transform Connections => connections;

        public bool SetTheme(JoinTableTheme theme)
        {
            return students != null && marks != null && result != null
                && students.SetTheme(theme) && marks.SetTheme(theme) && result.SetTheme(theme);
        }

        public bool Initialize()
        {
            if (students == null || marks == null || result == null || connections == null ||
                students == marks || students == result || marks == result)
            {
                Debug.LogError("Assign three distinct JOIN views and a Connections container.", this);
                return false;
            }
            return students.Initialize() && marks.Initialize() && result.Initialize();
        }
    }
}
