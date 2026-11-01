using System;
using System.Collections.Generic;

namespace ARSQLExplorer.SQLJoin
{
    public static class JoinCalculator
    {
        /// <summary>
        /// Joins Students.StudentId = Marks.StudentId, preserving every matching pair.
        /// Order: Students input order, then Marks input order for each student,
        /// followed by unmatched Marks in input order when included by the join.
        /// Inputs must contain non-null rows. Input collections are never modified.
        /// </summary>
        public static IReadOnlyList<JoinResult> Calculate(
            IReadOnlyList<StudentRecord> students,
            IReadOnlyList<MarkRecord> marks,
            JoinType joinType)
        {
            if (students == null) throw new ArgumentNullException(nameof(students));
            if (marks == null) throw new ArgumentNullException(nameof(marks));
            if (!Enum.IsDefined(typeof(JoinType), joinType))
                throw new ArgumentOutOfRangeException(nameof(joinType));

            for (int i = 0; i < students.Count; i++)
                if (students[i] == null)
                    throw new ArgumentException("Student rows cannot be null.", nameof(students));
            for (int i = 0; i < marks.Count; i++)
                if (marks[i] == null)
                    throw new ArgumentException("Marks rows cannot be null.", nameof(marks));

            bool includeStudents = joinType == JoinType.Left || joinType == JoinType.FullOuter;
            bool includeMarks = joinType == JoinType.Right || joinType == JoinType.FullOuter;
            var results = new List<JoinResult>();
            var matchedMarks = new bool[marks.Count];

            foreach (StudentRecord student in students)
            {
                bool matched = false;
                for (int i = 0; i < marks.Count; i++)
                {
                    if (student.StudentId != marks[i].StudentId) continue;

                    results.Add(new JoinResult(student, marks[i]));
                    matchedMarks[i] = true;
                    matched = true;
                }

                if (!matched && includeStudents)
                    results.Add(new JoinResult(student, null));
            }

            if (includeMarks)
                for (int i = 0; i < marks.Count; i++)
                    if (!matchedMarks[i])
                        results.Add(new JoinResult(null, marks[i]));

            return results.AsReadOnly();
        }
    }
}
