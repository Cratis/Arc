// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Represents the types a generation run has found to be involved, answering membership without scanning.
/// </summary>
/// <remarks>
/// Collecting asks whether a type is already known for every property of every type it walks. Against a plain
/// list that is a scan per question, which grows with the square of the number of types on a large solution.
/// Order of insertion is kept, so everything enumerating the types sees what it saw before.
/// </remarks>
sealed class TypesInvolved : IList<Type>
{
    readonly List<Type> _types = [];
    readonly HashSet<Type> _members = [];

    /// <inheritdoc/>
    public int Count => _types.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public Type this[int index]
    {
        get => _types[index];
        set
        {
            _members.Remove(_types[index]);
            _types[index] = value;
            _members.Add(value);
        }
    }

    /// <inheritdoc/>
    public void Add(Type item)
    {
        _types.Add(item);
        _members.Add(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _types.Clear();
        _members.Clear();
    }

    /// <summary>
    /// Removes every repeated occurrence of a type, keeping the first.
    /// </summary>
    public void RemoveDuplicates()
    {
        var distinct = _types.Distinct().ToList();
        _types.Clear();
        _types.AddRange(distinct);
    }

    /// <inheritdoc/>
    public bool Contains(Type item) => _members.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(Type[] array, int arrayIndex) => _types.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<Type> GetEnumerator() => _types.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(Type item) => _types.IndexOf(item);

    /// <inheritdoc/>
    public void Insert(int index, Type item)
    {
        _types.Insert(index, item);
        _members.Add(item);
    }

    /// <inheritdoc/>
    public bool Remove(Type item)
    {
        var removed = _types.Remove(item);
        if (removed && !_types.Contains(item))
        {
            _members.Remove(item);
        }

        return removed;
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        var item = _types[index];
        _types.RemoveAt(index);
        if (!_types.Contains(item))
        {
            _members.Remove(item);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
