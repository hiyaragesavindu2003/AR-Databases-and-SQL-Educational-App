using System;
using System.Collections.Generic;

namespace ARSQLExplorer.SQLJoin
{
    // Presentation cursor only: JOIN decisions remain in JoinCalculator.
    public sealed class JoinPlayback
    {
        public IReadOnlyList<JoinResult> Results { get; }
        public int ShownCount { get; private set; }
        public bool CanAdvance => ShownCount < Results.Count;

        public JoinPlayback(IReadOnlyList<JoinResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            var snapshot = new List<JoinResult>(results.Count);
            foreach (var result in results)
            {
                if (result == null) throw new ArgumentException("Result rows cannot be null.", nameof(results));
                snapshot.Add(result);
            }
            Results = snapshot.AsReadOnly();
        }

        public bool TryAdvance(out JoinResult result)
        {
            result = null;
            if (!CanAdvance) return false;
            result = Results[ShownCount++];
            return true;
        }

        public void ShowAll() => ShownCount = Results.Count;
        public void Reset() => ShownCount = 0;
    }
}
