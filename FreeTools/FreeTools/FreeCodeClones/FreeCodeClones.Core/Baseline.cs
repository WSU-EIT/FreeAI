using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FreeCodeClones.Core;

/// <summary>An accepted clone class: its id and the (required) reason it is allowed to stay.</summary>
public sealed class BaselineEntry
{
    public BaselineEntry(string id, string reason)
    {
        Id = id;
        Reason = reason;
    }

    public string Id { get; }

    public string Reason { get; }
}

/// <summary>The outcome of comparing a scan with a baseline.</summary>
public sealed class BaselineCheck
{
    internal BaselineCheck(IReadOnlyList<CloneClass> newClasses, IReadOnlyList<CloneClass> accepted, IReadOnlyList<BaselineEntry> stale)
    {
        New = newClasses;
        Accepted = accepted;
        Stale = stale;
    }

    /// <summary>Classes whose id is not in the baseline.</summary>
    public IReadOnlyList<CloneClass> New { get; }

    public IReadOnlyList<CloneClass> Accepted { get; }

    /// <summary>Baseline entries that no longer match anything (the duplicate was removed, or changed shape).</summary>
    public IReadOnlyList<BaselineEntry> Stale { get; }

    /// <summary>1 when there are new classes (for --fail-on-new), else 0.</summary>
    public int ExitCode => New.Count > 0 ? 1 : 0;
}

/// <summary>
/// Reads, writes and checks a baseline of accepted clone classes, so the detector can gate a build:
/// <code>
/// { "accepted": [ { "id": "3f2a9c0d1b7e4a55", "reason": "SignalR group join, the framework pattern", "example": "..." } ] }
/// </code>
/// Ids are content hashes, so an accepted exact class stays accepted when it moves; a near class gets a new id
/// when its membership changes.
/// </summary>
public static class Baseline
{
    public const string TodoReason = "TODO: justify";

    public static BaselineCheck Check(IReadOnlyList<CloneClass> classes, IReadOnlyList<BaselineEntry> baseline)
    {
        var ids = new HashSet<string>(baseline.Select(b => b.Id), StringComparer.OrdinalIgnoreCase);
        var found = new HashSet<string>(classes.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
        return new BaselineCheck(
            classes.Where(c => !ids.Contains(c.Id)).ToList(),
            classes.Where(c => ids.Contains(c.Id)).ToList(),
            baseline.Where(b => !found.Contains(b.Id)).ToList());
    }

    /// <summary>Parses a baseline file. Throws <see cref="FormatException"/> when it is malformed or an entry has no reason.</summary>
    public static List<BaselineEntry> Read(string json)
    {
        if (!(new MiniJson(json).Parse() is Dictionary<string, object?> root)
            || !root.TryGetValue("accepted", out object? accepted)
            || !(accepted is List<object?> items)) {
            throw new FormatException("A baseline is an object with an \"accepted\" array.");
        }

        var result = new List<BaselineEntry>();
        foreach (object? item in items) {
            if (!(item is Dictionary<string, object?> entry) || !(entry.TryGetValue("id", out object? id) && id is string idText) || idText.Length == 0) {
                throw new FormatException("Every baseline entry needs an \"id\" string.");
            }

            if (!(entry.TryGetValue("reason", out object? reason) && reason is string reasonText) || reasonText.Trim().Length == 0) {
                throw new FormatException($"Baseline entry {idText} has no \"reason\". Say why the duplicate is acceptable.");
            }

            result.Add(new BaselineEntry(idText, reasonText));
        }

        return result;
    }

    /// <summary>A baseline accepting every given class, with reason "TODO: justify" for a human to edit.</summary>
    public static string Write(IEnumerable<CloneClass> classes)
    {
        var j = new CloneReport.JsonWriter();
        j.Open('{');
        j.Key("accepted");
        j.Open('[');
        foreach (CloneClass c in classes) {
            j.Open('{');
            j.Property("id", c.Id);
            j.Property("kind", CloneReport.Kind(c.Kind));
            j.Property("reason", TodoReason);
            j.Property("example", string.Join(", ", c.Members.Select(m => m.Location)));
            j.Close('}');
        }

        j.Close(']');
        j.Close('}');
        return j.ToString();
    }

    /// <summary>A minimal JSON reader (objects, arrays, strings, numbers, true/false/null), enough for a baseline file.</summary>
    private sealed class MiniJson
    {
        private readonly string _s;
        private int _i;

        public MiniJson(string s) => _s = s;

        public object? Parse()
        {
            object? value = ParseValue();
            SkipWhitespace();
            if (_i != _s.Length) {
                throw Error("unexpected text after the JSON value");
            }

            return value;
        }

        private FormatException Error(string message) => new($"Baseline JSON, position {_i}: {message}.");

        private void Expect(char c)
        {
            SkipWhitespace();
            if (_i >= _s.Length || _s[_i] != c) {
                throw Error($"expected '{c}'");
            }

            _i++;
        }

        private List<object?> ParseArray()
        {
            Expect('[');
            var list = new List<object?>();
            SkipWhitespace();
            if (_i < _s.Length && _s[_i] == ']') {
                _i++;
                return list;
            }

            while (true) {
                list.Add(ParseValue());
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == ',') {
                    _i++;
                    continue;
                }

                Expect(']');
                return list;
            }
        }

        private Dictionary<string, object?> ParseObject()
        {
            Expect('{');
            var map = new Dictionary<string, object?>(StringComparer.Ordinal);
            SkipWhitespace();
            if (_i < _s.Length && _s[_i] == '}') {
                _i++;
                return map;
            }

            while (true) {
                SkipWhitespace();
                string key = ParseString();
                Expect(':');
                map[key] = ParseValue();
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == ',') {
                    _i++;
                    continue;
                }

                Expect('}');
                return map;
            }
        }

        private string ParseString()
        {
            Expect('"');
            var sb = new StringBuilder();
            while (_i < _s.Length && _s[_i] != '"') {
                char c = _s[_i++];
                if (c != '\\') {
                    sb.Append(c);
                    continue;
                }

                if (_i >= _s.Length) {
                    break;
                }

                char e = _s[_i++];
                switch (e) {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u' when _i + 4 <= _s.Length:
                        sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        _i += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }

            Expect('"');
            return sb.ToString();
        }

        private object? ParseValue()
        {
            SkipWhitespace();
            if (_i >= _s.Length) {
                throw Error("unexpected end");
            }

            char c = _s[_i];
            switch (c) {
                case '{': return ParseObject();
                case '[': return ParseArray();
                case '"': return ParseString();
            }

            foreach ((string word, object? value) in new (string, object?)[] { ("true", true), ("false", false), ("null", null) }) {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) == 0) {
                    _i += word.Length;
                    return value;
                }
            }

            int start = _i;
            while (_i < _s.Length && "+-.0123456789eE".IndexOf(_s[_i]) >= 0) {
                _i++;
            }

            if (start == _i || !double.TryParse(_s.Substring(start, _i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)) {
                throw Error("expected a value");
            }

            return number;
        }

        private void SkipWhitespace()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) {
                _i++;
            }
        }
    }
}