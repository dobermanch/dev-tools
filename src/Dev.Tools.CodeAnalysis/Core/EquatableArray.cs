using System.Collections;

namespace Dev.Tools.CodeAnalysis.Core;

/// <summary>
/// An array with value equality, so a record holding one can be compared by content.
///
/// Roslyn decides whether to re-run a pipeline step by comparing the previous output with the new
/// one. A plain <c>T[]</c> on a record compares by reference, so every array-valued model looks
/// different on every compilation and nothing downstream is ever cached. Wrapping the array makes
/// the models genuinely comparable, which is what keeps the generators incremental.
/// </summary>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _items;

    public EquatableArray(T[]? items) => _items = items;

    public static EquatableArray<T> Empty { get; } = new([]);

    public int Length => _items?.Length ?? 0;

    public T this[int index] => _items![index];

    public bool Equals(EquatableArray<T> other)
    {
        var left = _items ?? [];
        var right = other._items ?? [];

        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in _items ?? [])
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static implicit operator EquatableArray<T>(T[]? items) => new(items);
}

public static class EquatableArray
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source)
        where T : IEquatable<T>
        => new(source as T[] ?? source.ToArray());
}
