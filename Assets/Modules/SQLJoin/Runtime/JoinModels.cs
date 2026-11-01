using System;

namespace ARSQLExplorer.SQLJoin
{
    public enum JoinType
    {
        Inner,
        Left,
        Right,
        FullOuter
    }

    // Plain immutable classes avoid Unity-specific serialization and record support.
    public sealed class StudentRecord
    {
        public int StudentId { get; }
        public string Name { get; }

        public StudentRecord(int studentId, string name)
        {
            StudentId = studentId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    public sealed class MarkRecord
    {
        public int MarkId { get; }
        public int StudentId { get; }
        public string Subject { get; }
        public int Score { get; }

        public MarkRecord(int markId, int studentId, string subject, int score)
        {
            MarkId = markId;
            StudentId = studentId;
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            Score = score;
        }
    }

    public sealed class JoinResult
    {
        // Either reference may be null for an outer join; both cannot be null.
        // Retain the actual source objects so views can trace results to input rows.
        public StudentRecord Student { get; }
        public MarkRecord Mark { get; }

        public JoinResult(StudentRecord student, MarkRecord mark)
        {
            if (student == null && mark == null)
                throw new ArgumentException("A result must have at least one source row.");

            Student = student;
            Mark = mark;
        }
    }
}
