using System;
using System.Linq;
using NUnit.Framework;

namespace ARSQLExplorer.SQLJoin.Tests
{
    public class JoinCalculatorTests
    {
        [TestCase(JoinType.Inner, 3, false, false)]
        [TestCase(JoinType.Left, 4, true, false)]
        [TestCase(JoinType.Right, 4, false, true)]
        [TestCase(JoinType.FullOuter, 5, true, true)]
        public void DemoJoinsPreserveMatchesAndOnlyRequiredUnmatchedRows(
            JoinType type, int count, bool includesStudent, bool includesMark)
        {
            var results = JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, type);
            Assert.That(results.Count, Is.EqualTo(count));
            var amal = results.Where(r => r.Student != null && r.Student.StudentId == 101).ToArray();
            Assert.That(amal.Select(r => r.Mark.MarkId), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(amal[0].Student, Is.SameAs(JoinDemoData.Students[0]));
            Assert.That(amal[0].Mark, Is.SameAs(JoinDemoData.Marks[0]));
            Assert.That(results.Count(r => r.Student == JoinDemoData.Students[2] && r.Mark == null),
                Is.EqualTo(includesStudent ? 1 : 0));
            Assert.That(results.Count(r => r.Student == null && r.Mark == JoinDemoData.Marks[3]),
                Is.EqualTo(includesMark ? 1 : 0));
            Assert.That(results.Where(r => r.Student != null && r.Mark != null)
                .All(r => r.Student.StudentId == r.Mark.StudentId), Is.True);
        }

        [Test]
        public void FullJoinHasStableTeachingOrderAndRetainsSources()
        {
            for (int run = 0; run < 2; run++)
            {
                var results = JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, JoinType.FullOuter);
                Assert.That(results.Select(r => r.Student == null ? (int?)null : r.Student.StudentId),
                    Is.EqualTo(new int?[] { 101, 101, 102, 103, null }));
                Assert.That(results.Select(r => r.Mark == null ? (int?)null : r.Mark.MarkId),
                    Is.EqualTo(new int?[] { 1, 2, 3, null, 4 }));
            }
        }

        [Test]
        public void OrderingFollowsInputsRatherThanSortingIds()
        {
            var students = JoinDemoData.Students.Reverse().ToArray();
            var marks = JoinDemoData.Marks.Reverse().ToArray();
            var results = JoinCalculator.Calculate(students, marks, JoinType.FullOuter);
            Assert.That(results.Select(r => r.Mark == null ? (int?)null : r.Mark.MarkId),
                Is.EqualTo(new int?[] { null, 3, 2, 1, 4 }));
            Assert.That(students[0], Is.SameAs(JoinDemoData.Students[2]));
            Assert.That(marks[0], Is.SameAs(JoinDemoData.Marks[3]));
        }

        [TestCase(JoinType.Inner, 0, 0)]
        [TestCase(JoinType.Left, 3, 0)]
        [TestCase(JoinType.Right, 0, 4)]
        [TestCase(JoinType.FullOuter, 3, 4)]
        public void EmptyInputsRespectPreservedSide(JoinType type, int studentsOnly, int marksOnly)
        {
            Assert.That(JoinCalculator.Calculate(JoinDemoData.Students, new MarkRecord[0], type).Count,
                Is.EqualTo(studentsOnly));
            Assert.That(JoinCalculator.Calculate(new StudentRecord[0], JoinDemoData.Marks, type).Count,
                Is.EqualTo(marksOnly));
            Assert.That(JoinCalculator.Calculate(new StudentRecord[0], new MarkRecord[0], type), Is.Empty);
        }

        [Test]
        public void RepeatedMatchingRowsAreNotDeduplicated()
        {
            var student = JoinDemoData.Students[0];
            var mark = JoinDemoData.Marks[0];
            var results = JoinCalculator.Calculate(new[] { student, student }, new[] { mark, mark }, JoinType.Inner);
            Assert.That(results.Count, Is.EqualTo(4));
            Assert.That(results.All(r => r.Student == student && r.Mark == mark), Is.True);
        }

        [Test]
        public void InvalidInputsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => JoinCalculator.Calculate(null, JoinDemoData.Marks, JoinType.Inner));
            Assert.Throws<ArgumentNullException>(() => JoinCalculator.Calculate(JoinDemoData.Students, null, JoinType.Inner));
            Assert.Throws<ArgumentOutOfRangeException>(() => JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, (JoinType)99));
            Assert.Throws<ArgumentException>(() => JoinCalculator.Calculate(new StudentRecord[] { null }, JoinDemoData.Marks, JoinType.Inner));
            Assert.Throws<ArgumentException>(() => JoinCalculator.Calculate(JoinDemoData.Students, new MarkRecord[] { null }, JoinType.Inner));
            Assert.Throws<ArgumentException>(() => new JoinResult(null, null));
        }
    }
}
