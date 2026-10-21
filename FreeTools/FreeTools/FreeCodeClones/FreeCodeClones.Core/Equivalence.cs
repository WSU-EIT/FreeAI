using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeCodeClones.Core;

/// <summary>What one call produced: a value, or the type of the exception it threw.</summary>
public readonly struct Outcome<T>
{
    public Outcome(T value)
    {
        Value = value;
        Exception = null;
    }

    public Outcome(Type exception)
    {
        Value = default;
        Exception = exception;
    }

    public T? Value { get; }

    public Type? Exception { get; }

    public bool Threw => Exception is not null;

    public override string ToString() => Threw ? "throws " + Exception!.Name : Value is null ? "null" : "\"" + Value + "\"";
}

/// <summary>An input on which the two functions disagreed, with what each produced.</summary>
public sealed class Difference<TIn, TOut>
{
    internal Difference(TIn input, Outcome<TOut> a, Outcome<TOut> b)
    {
        Input = input;
        A = a;
        B = b;
    }

    public TIn Input { get; }

    public Outcome<TOut> A { get; }

    public Outcome<TOut> B { get; }

    public override string ToString() => $"{(Input is null ? "null" : "\"" + Input + "\"")}: a {A}, b {B}";
}

/// <summary>
/// Differential testing: runs two functions on the same inputs and reports where they disagree. This is the
/// behavioural counterpart of the clone detector, for "do these two methods actually do the same thing?",
/// which shape matching cannot answer. It is evidence on the inputs tried, not a proof for all inputs: general
/// functional equivalence is undecidable. Exhaustive inputs (<see cref="Inputs.Exhaustive{T1}"/>) do make it a
/// proof for that finite domain.
/// </summary>
public static class Equivalence
{
    /// <summary>
    /// The inputs where the outputs differ, or where exactly one of the two threw. An exception counts as an
    /// output and is compared by type, so two functions that both throw ArgumentNullException on null agree.
    /// </summary>
    public static IReadOnlyList<Difference<TIn, TOut>> Compare<TIn, TOut>(
        Func<TIn, TOut> a,
        Func<TIn, TOut> b,
        IEnumerable<TIn> inputs,
        IEqualityComparer<TOut>? cmp = null)
    {
        IEqualityComparer<TOut> comparer = cmp ?? EqualityComparer<TOut>.Default;
        var differences = new List<Difference<TIn, TOut>>();
        foreach (TIn input in inputs) {
            Outcome<TOut> ra = Run(a, input);
            Outcome<TOut> rb = Run(b, input);
            bool same = ra.Threw || rb.Threw
                ? ra.Exception == rb.Exception
                : comparer.Equals(ra.Value!, rb.Value!);
            if (!same) {
                differences.Add(new Difference<TIn, TOut>(input, ra, rb));
            }
        }

        return differences;
    }

    private static Outcome<TOut> Run<TIn, TOut>(Func<TIn, TOut> f, TIn input)
    {
        try {
            return new Outcome<TOut>(f(input));
        } catch (Exception ex) {
            return new Outcome<TOut>(ex.GetType());
        }
    }
}

/// <summary>Input generators for <see cref="Equivalence.Compare{TIn, TOut}"/>.</summary>
public static class Inputs
{
    /// <summary>
    /// Cases around a prefix such as "refs/heads/": the prefix itself, prefix + "x", the upper-cased prefix, the
    /// prefix in the middle ("x/refs/heads/y"), doubled, after a space, and without its trailing separator.
    /// </summary>
    public static string[] AroundPrefix(string prefix) => new[]
    {
        prefix,
        prefix + "x",
        prefix.ToUpperInvariant() + "x",
        "x/" + prefix + "y",
        prefix + prefix + "x",
        " " + prefix + "x",
        prefix.TrimEnd('/', '\\'),
        prefix + "x/"
    };

    /// <summary>Every value of one bool or enum type (nullable adds null): a truth table of one parameter.</summary>
    public static IEnumerable<T1> Exhaustive<T1>() => Values<T1>();

    /// <summary>Every combination of two bool/enum parameters.</summary>
    public static IEnumerable<(T1, T2)> Exhaustive<T1, T2>() =>
        from a in Values<T1>()
        from b in Values<T2>()
        select (a, b);

    /// <summary>Every combination of three bool/enum parameters.</summary>
    public static IEnumerable<(T1, T2, T3)> Exhaustive<T1, T2, T3>() =>
        from a in Values<T1>()
        from b in Values<T2>()
        from c in Values<T3>()
        select (a, b, c);

    /// <summary>
    /// Edge-case strings for path- and name-like inputs: null, empty, whitespace, separators at either end, mixed
    /// case, unicode, a very long string, then the caller's own cases (see <see cref="AroundPrefix"/>). Duplicates
    /// are removed; order is fixed.
    /// </summary>
    public static IReadOnlyList<string?> Strings(params string[] extra)
    {
        var cases = new List<string?>
        {
            null, "", " ", "\t", "  x  ", "\r\n",
            "/", "//", "\\", "/x", "x/", "\\x", "x\\", "/x/", "x/y", "x\\y", "x//y", ".", "..", "./x", "../x",
            "x", "X", "MiXeD", "mixed", "MIXED",
            "ünïcödé", "名前", "İstanbul", "é", "😀",
            new string('a', 10_000)
        };

        cases.AddRange(extra);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        bool sawNull = false;
        var result = new List<string?>();
        foreach (string? s in cases) {
            if (s is null ? !sawNull : seen.Add(s)) {
                sawNull |= s is null;
                result.Add(s);
            }
        }

        return result;
    }

    /// <summary>Every value of a bool, enum, or nullable bool/enum type.</summary>
    public static IReadOnlyList<T> Values<T>()
    {
        Type type = typeof(T);
        Type? underlying = Nullable.GetUnderlyingType(type);
        var values = new List<object?>();
        if (underlying is not null) {
            values.Add(null);
            type = underlying;
        }

        if (type == typeof(bool)) {
            values.Add(false);
            values.Add(true);
        } else if (type.IsEnum) {
            values.AddRange(Enum.GetValues(type).Cast<object>());
        } else {
            throw new ArgumentException($"Exhaustive inputs cover bool and enum types only, not {typeof(T).Name}.");
        }

        return values.Select(v => (T)v!).ToList();
    }
}