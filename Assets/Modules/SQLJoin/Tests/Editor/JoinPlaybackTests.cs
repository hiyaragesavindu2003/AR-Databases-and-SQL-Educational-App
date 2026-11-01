using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ARSQLExplorer.SQLJoin.Tests
{
    public class JoinPlaybackTests
    {
        [TestCase(JoinType.Inner, 3)]
        [TestCase(JoinType.Left, 4)]
        [TestCase(JoinType.Right, 4)]
        [TestCase(JoinType.FullOuter, 5)]
        public void EachStepReturnsExactlyOneOriginalResultInOrder(JoinType type, int count)
        {
            var results = JoinCalculator.Calculate(JoinDemoData.Students, JoinDemoData.Marks, type);
            var playback = new JoinPlayback(results);
            Assert.That(playback.ShownCount, Is.Zero);
            for (int i = 0; i < count; i++)
            {
                Assert.That(playback.TryAdvance(out var result), Is.True);
                Assert.That(result, Is.SameAs(results[i]));
                Assert.That(playback.ShownCount, Is.EqualTo(i + 1));
            }
            Assert.That(playback.TryAdvance(out var extra), Is.False);
            Assert.That(extra, Is.Null);
            Assert.That(playback.ShownCount, Is.EqualTo(count));
        }

        [Test]
        public void ResetReplaysFirstResultAndShowAllCannotDuplicateRows()
        {
            var playback = new JoinPlayback(JoinCalculator.Calculate(
                JoinDemoData.Students, JoinDemoData.Marks, JoinType.FullOuter));
            playback.TryAdvance(out var first);
            playback.ShowAll();
            playback.ShowAll();
            Assert.That(playback.ShownCount, Is.EqualTo(5));
            Assert.That(playback.CanAdvance, Is.False);
            playback.Reset();
            Assert.That(playback.ShownCount, Is.Zero);
            playback.TryAdvance(out var replayed);
            Assert.That(replayed, Is.SameAs(first));
        }

        [Test]
        public void SnapshotRetainsNullSidesAndSourceReferences()
        {
            var input = new List<JoinResult>(JoinCalculator.Calculate(
                JoinDemoData.Students, JoinDemoData.Marks, JoinType.FullOuter));
            var playback = new JoinPlayback(input);
            input.Clear();
            Assert.That(playback.Results.Count, Is.EqualTo(5));
            Assert.That(playback.Results[3].Student, Is.SameAs(JoinDemoData.Students[2]));
            Assert.That(playback.Results[3].Mark, Is.Null);
            Assert.That(playback.Results[4].Student, Is.Null);
            Assert.That(playback.Results[4].Mark, Is.SameAs(JoinDemoData.Marks[3]));
        }

        [Test]
        public void EmptyPlaybackIsCompleteAndRejectsInvalidInput()
        {
            var playback = new JoinPlayback(new JoinResult[0]);
            playback.Reset();
            playback.ShowAll();
            Assert.That(playback.TryAdvance(out _), Is.False);
            Assert.That(playback.ShownCount, Is.Zero);
            Assert.Throws<ArgumentNullException>(() => new JoinPlayback(null));
            Assert.Throws<ArgumentException>(() => new JoinPlayback(new JoinResult[] { null }));
        }
    }
}
