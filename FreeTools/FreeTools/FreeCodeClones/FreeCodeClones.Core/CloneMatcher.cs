using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeCodeClones.Core;

public enum CloneKind
{
    Exact,
    Near
}

/// <summary>A group of units whose flattened forms match exactly, or (transitively) nearly.</summary>
public sealed class CloneClass
{
    internal CloneClass(string id, CloneKind kind, double similarity, IReadOnlyList<CodeUnit> members)
    {
        Id = id;
        Kind = kind;
        Similarity = similarity;
        Members = members;
    }

    /// <summary>Stable id: exact = a prefix of the SHA-256 of the normalised stream; near = a hash of its members' hashes.</summary>
    public string Id { get; }

    public CloneKind Kind { get; }

    /// <summary>1 for exact classes; for near classes the lowest similarity of the pairs that joined it.</summary>
    public double Similarity { get; }

    /// <summary>Occurrences, sorted by path then position.</summary>
    public IReadOnlyList<CodeUnit> Members { get; }

    public int Tokens => Members[0].Tokens.Count;

    public int Statements => Members[0].Statements;

    /// <summary>Tokens times occurrences: how much text the duplication costs. Classes are reported largest first.</summary>
    public long Size => (long)Tokens * Members.Count;

    public string Normalized => string.Join(" ", Members[0].Tokens);
}

/// <summary>
/// Finds clone classes among units. Exact: equal SHA-256 of the normalised stream. Near: Jaccard similarity of
/// k-gram shingles (k = 5) at or above the threshold, candidates found through winnowed fingerprints and grouped
/// transitively. Near-miss compares member units only (methods, accessors, lambdas...), not statement windows.
/// A class is dropped when each of its occurrences sits inside a distinct, larger occurrence of another class,
/// so only maximal windows are reported.
/// </summary>
public static class CloneMatcher
{
    private const int K = 5;
    private const int WinnowWindow = 4;

    public static List<CloneClass> Find(UnitSet units, Normalizer normalizer, CloneOptions options)
    {
        var classes = new List<CloneClass>();
        classes.AddRange(ExactClasses(units, normalizer));
        classes.AddRange(NearClasses(units.Members, options.Similarity));

        return Prune(classes)
            .OrderByDescending(c => c.Size)
            .ThenBy(c => c.Kind)
            .ThenBy(c => c.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static bool CoveredBy(CloneClass inner, CloneClass outer)
    {
        var used = new bool[outer.Members.Count];
        bool strict = false;
        foreach (CodeUnit u in inner.Members) {
            int found = -1;
            for (int k = 0; k < outer.Members.Count; k++) {
                if (!used[k] && outer.Members[k].Contains(u)) {
                    found = k;
                    break;
                }
            }

            if (found < 0) {
                return false;
            }

            used[found] = true;
            strict |= outer.Members[found].Start != u.Start || outer.Members[found].End != u.End;
        }

        return strict;
    }

    private static IEnumerable<CloneClass> ExactClasses(UnitSet units, Normalizer normalizer)
    {
        var byHash = new Dictionary<string, List<CodeUnit>>(StringComparer.Ordinal);
        foreach (CodeUnit m in units.Members) {
            AddTo(byHash, m.Hash, m);
        }

        // Windows are only flattened when their 64-bit hash recurs; the SHA-256 then confirms the match.
        foreach (IGrouping<ulong, WindowCandidate> group in units.Windows.GroupBy(w => w.Hash)) {
            if (group.Skip(1).Any()) {
                foreach (WindowCandidate w in group) {
                    CodeUnit unit = normalizer.Materialize(units, w);
                    AddTo(byHash, unit.Hash, unit);
                }
            }
        }

        foreach (KeyValuePair<string, List<CodeUnit>> entry in byHash.OrderBy(e => e.Key, StringComparer.Ordinal)) {
            List<CodeUnit> members = NonOverlapping(entry.Value);
            if (members.Count >= 2) {
                yield return new CloneClass(entry.Key.Substring(0, 16), CloneKind.Exact, 1.0, members);
            }
        }
    }

    private static double Jaccard(HashSet<ulong> a, HashSet<ulong> b)
    {
        if (a.Count == 0 || b.Count == 0) {
            return 0;
        }

        HashSet<ulong> small = a.Count <= b.Count ? a : b;
        HashSet<ulong> large = a.Count <= b.Count ? b : a;
        int shared = small.Count(large.Contains);
        return (double)shared / (a.Count + b.Count - shared);
    }

    private static IEnumerable<CloneClass> NearClasses(List<CodeUnit> members, double threshold)
    {
        int n = members.Count;
        var tokenIds = new Dictionary<string, int>(StringComparer.Ordinal);
        var shingles = new HashSet<ulong>[n];
        var prints = new List<ulong>[n];
        var index = new Dictionary<ulong, List<int>>();
        for (int i = 0; i < n; i++) {
            string[] near = members[i].NearTokens!;
            var hashes = new List<ulong>();
            for (int s = 0; s + K <= near.Length; s++) {
                ulong h = Hashing.Seed;
                for (int j = s; j < s + K; j++) {
                    if (!tokenIds.TryGetValue(near[j], out int id)) {
                        id = tokenIds.Count + 1;
                        tokenIds[near[j]] = id;
                    }

                    h = Hashing.Mix(h, (ulong)id);
                }

                hashes.Add(h);
            }

            shingles[i] = new HashSet<ulong>(hashes);
            prints[i] = Winnow(hashes);
            foreach (ulong p in prints[i]) {
                if (!index.TryGetValue(p, out List<int>? postings)) {
                    postings = new List<int>();
                    index[p] = postings;
                }

                postings.Add(i);
            }
        }

        // Union-find over the near pairs, remembering the weakest link of each component.
        var parent = Enumerable.Range(0, n).ToArray();
        var edges = new List<(int A, int B, double J)>();
        for (int a = 0; a < n; a++) {
            var shared = new Dictionary<int, int>();
            foreach (ulong p in prints[a]) {
                foreach (int b in index[p]) {
                    if (b > a) {
                        shared[b] = shared.TryGetValue(b, out int c) ? c + 1 : 1;
                    }
                }
            }

            foreach (KeyValuePair<int, int> pair in shared.OrderBy(p => p.Key)) {
                int b = pair.Key;
                CodeUnit ua = members[a];
                CodeUnit ub = members[b];
                if (ua.Hash == ub.Hash || ua.Overlaps(ub)) {
                    continue; // exact clones are reported as such; nested units are not copies of each other
                }

                int sa = shingles[a].Count;
                int sb = shingles[b].Count;
                if (Math.Min(sa, sb) < threshold * Math.Max(sa, sb)
                    || pair.Value < 0.5 * Math.Min(prints[a].Count, prints[b].Count)) {
                    continue; // cannot reach the threshold
                }

                double j = Jaccard(shingles[a], shingles[b]);
                if (j >= threshold) {
                    edges.Add((a, b, j));
                    parent[Find(parent, a)] = Find(parent, b);
                }
            }
        }

        var components = new Dictionary<int, List<int>>();
        var weakest = new Dictionary<int, double>();
        foreach ((int a, int b, double j) in edges) {
            int root = Find(parent, a);
            weakest[root] = weakest.TryGetValue(root, out double w) ? Math.Min(w, j) : j;
        }

        for (int i = 0; i < n; i++) {
            int root = Find(parent, i);
            if (weakest.ContainsKey(root)) {
                AddTo(components, root, i);
            }
        }

        foreach (KeyValuePair<int, List<int>> component in components) {
            List<CodeUnit> units = Sorted(component.Value.Select(i => members[i]));
            string id = Hashing.Sha256Hex("near\n" + string.Join("\n", units.Select(u => u.Hash).OrderBy(h => h, StringComparer.Ordinal)));
            yield return new CloneClass(id.Substring(0, 16), CloneKind.Near, Math.Round(weakest[component.Key], 4), units);
        }
    }

    private static void AddTo<TKey>(Dictionary<TKey, List<CodeUnit>> map, TKey key, CodeUnit unit) where TKey : notnull
    {
        if (!map.TryGetValue(key, out List<CodeUnit>? list)) {
            list = new List<CodeUnit>();
            map[key] = list;
        }

        list.Add(unit);
    }

    private static void AddTo(Dictionary<int, List<int>> map, int key, int value)
    {
        if (!map.TryGetValue(key, out List<int>? list)) {
            list = new List<int>();
            map[key] = list;
        }

        list.Add(value);
    }

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i) {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }

        return i;
    }

    /// <summary>Drops occurrences that overlap an earlier one in the same class (repetitive code, nested spans).</summary>
    private static List<CodeUnit> NonOverlapping(IEnumerable<CodeUnit> units)
    {
        var kept = new List<CodeUnit>();
        foreach (CodeUnit u in Sorted(units)) {
            if (!kept.Any(k => k.Overlaps(u))) {
                kept.Add(u);
            }
        }

        return kept;
    }

    private static List<CloneClass> Prune(List<CloneClass> classes)
    {
        var byPart = new Dictionary<int, List<(CodeUnit Unit, int Class)>>();
        for (int c = 0; c < classes.Count; c++) {
            foreach (CodeUnit u in classes[c].Members) {
                if (!byPart.TryGetValue(u.Part.Id, out List<(CodeUnit, int)>? list)) {
                    list = new List<(CodeUnit, int)>();
                    byPart[u.Part.Id] = list;
                }

                list.Add((u, c));
            }
        }

        var kept = new List<CloneClass>();
        for (int c = 0; c < classes.Count; c++) {
            CloneClass inner = classes[c];
            CodeUnit first = inner.Members[0];
            bool subsumed = byPart[first.Part.Id]
                .Where(x => x.Class != c && x.Unit.Contains(first))
                .Select(x => x.Class)
                .Distinct()
                .Any(d => classes[d].Members.Count >= inner.Members.Count && CoveredBy(inner, classes[d]));
            if (!subsumed) {
                kept.Add(inner);
            }
        }

        return kept;
    }

    private static List<CodeUnit> Sorted(IEnumerable<CodeUnit> units) => units
        .OrderBy(u => u.Path, StringComparer.Ordinal)
        .ThenBy(u => u.Part.Id)
        .ThenBy(u => u.Start)
        .ThenByDescending(u => u.End)
        .ToList();

    /// <summary>Winnowing: the smallest hash of every window of consecutive shingles (rightmost on ties).</summary>
    private static List<ulong> Winnow(List<ulong> hashes)
    {
        var picked = new HashSet<ulong>();
        if (hashes.Count <= WinnowWindow) {
            picked.UnionWith(hashes);
        } else {
            for (int i = 0; i + WinnowWindow <= hashes.Count; i++) {
                int min = i;
                for (int j = i + 1; j < i + WinnowWindow; j++) {
                    if (hashes[j] <= hashes[min]) {
                        min = j;
                    }
                }

                picked.Add(hashes[min]);
            }
        }

        return picked.ToList();
    }
}