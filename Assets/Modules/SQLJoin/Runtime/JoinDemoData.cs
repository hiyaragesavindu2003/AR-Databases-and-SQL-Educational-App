using System;
using System.Collections.Generic;

namespace ARSQLExplorer.SQLJoin
{
    public static class JoinDemoData
    {
        public static IReadOnlyList<StudentRecord> Students { get; } =
            Array.AsReadOnly(new[]
            {
                new StudentRecord(101, "Amal"),
                new StudentRecord(102, "Nimal"),
                new StudentRecord(103, "Kamal")
            });

        // Student 104 is deliberately unmatched. This teaching dataset does not
        // enforce a foreign-key constraint between Marks and Students.
        public static IReadOnlyList<MarkRecord> Marks { get; } =
            Array.AsReadOnly(new[]
            {
                new MarkRecord(1, 101, "SQL", 85),
                new MarkRecord(2, 101, "Databases", 78),
                new MarkRecord(3, 102, "SQL", 72),
                new MarkRecord(4, 104, "SQL", 90)
            });
    }
}
