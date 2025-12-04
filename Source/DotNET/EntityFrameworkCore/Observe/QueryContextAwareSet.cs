// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Arc.Queries;
using Cratis.Strings;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Represents a set that is aware of <see cref="QueryContext"/>.
/// </summary>
/// <remarks>This list is not mutated in a thread-safe way.</remarks>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
internal sealed class QueryContextAwareSet<TEntity> : IEnumerable<TEntity>
{
    readonly IEqualityComparer _idEqualityComparer;
    readonly Func<TEntity, object> _getId;
    LinkedList<(object Id, TEntity Entity)> _items = null!;
    QueryContext? _queryContext;
    int? _maxSize;
    Func<TEntity, object?> _getSortingField = _ => null;
    IComparer _sortingFieldComparer = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryContextAwareSet{TEntity}"/> class.
    /// </summary>
    /// <param name="queryContext">The query context.</param>
    /// <param name="idProperty">The id property.</param>
    public QueryContextAwareSet(QueryContext queryContext, PropertyInfo idProperty)
    {
        _idEqualityComparer = (typeof(EqualityComparer<>)
                .MakeGenericType(idProperty.PropertyType)
                .GetProperty(nameof(EqualityComparer<object>.Default), BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)
            as IEqualityComparer)!;
        ArgumentNullException.ThrowIfNull(_idEqualityComparer);
        _getId = entity =>
        {
            var id = idProperty.GetValue(entity);
            ArgumentNullException.ThrowIfNull(id);
            return id;
        };
        Initialize(queryContext);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryContextAwareSet{TEntity}"/> class.
    /// </summary>
    /// <param name="queryContext">The query context.</param>
    /// <remarks>Primarily used for testing.</remarks>
    public QueryContextAwareSet(QueryContext queryContext) : this(queryContext, typeof(TEntity).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!)
    {
    }

    /// <summary>
    /// Adds item to the set.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>True if added new item or changed stored item, false if not.</returns>
    public bool Add(TEntity item)
    {
        var value = (_getId(item), item);
        if (TryReplaceSameItem(value))
        {
            return true;
        }

        if (_maxSize is null || _items.Count < _maxSize)
        {
            AddWhenNotFull(value);
            return true;
        }
        return AddWhenFull(value);
    }

    /// <summary>
    /// Initializes the set from the query result.
    /// </summary>
    /// <param name="entities">The entities to initialize with.</param>
    public void InitializeWithEntities(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            _items.AddLast((_getId(entity), entity));
        }
    }

    /// <summary>
    /// Removes the entity with the given id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>True if removed an item, false if not.</returns>
    public bool Remove(object id)
    {
        var node = _items.First;
        while (node is not null)
        {
            if (_idEqualityComparer.Equals(node.Value.Id, id))
            {
                _items.Remove(node);
                return true;
            }
            node = node.Next;
        }
        return false;
    }

    /// <summary>
    /// Clears all items from the set and reinitializes with new entities.
    /// </summary>
    /// <param name="entities">The entities to reinitialize with.</param>
    public void ReinitializeWithEntities(IEnumerable<TEntity> entities)
    {
        _items.Clear();
        InitializeWithEntities(entities);
    }

    /// <inheritdoc/>
    public IEnumerator<TEntity> GetEnumerator() => _items.Select(node => node.Entity).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void Initialize(QueryContext newQueryContext)
    {
        var oldQueryContext = _queryContext;
        _queryContext = newQueryContext;
        _maxSize = null;
        if (_queryContext.Paging.IsPaged)
        {
            _maxSize = _queryContext.Paging.Size;
        }
        if (_maxSize < 1)
        {
            throw new ArgumentException("Page size must be greater than 0", nameof(newQueryContext));
        }

        var createNewStorage = oldQueryContext?.Paging.IsPaged == true && _maxSize < oldQueryContext.Paging.Size;
        if (oldQueryContext is not null && oldQueryContext.Sorting != Sorting.None &&
            (newQueryContext.Sorting.Direction != oldQueryContext.Sorting.Direction || newQueryContext.Sorting.Field != oldQueryContext.Sorting.Field))
        {
            createNewStorage = true;
        }

        _sortingFieldComparer = Comparer<object>.Default;

        if (SortingIsEnabled())
        {
            var sortingFieldProperty = typeof(TEntity).GetProperty(_queryContext.Sorting.Field.ToPascalCase(), BindingFlags.Instance | BindingFlags.Public)
                ?? throw new ArgumentException($"Sorting field could not be found on {typeof(TEntity)}", nameof(newQueryContext));
            _sortingFieldComparer = (typeof(Comparer<>)
                .MakeGenericType(sortingFieldProperty.PropertyType)
                .GetProperty(nameof(Comparer<object>.Default), BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)
                as IComparer)!;
            _getSortingField = entity =>
            {
                try
                {
                    return sortingFieldProperty.GetValue(entity);
                }
                catch (Exception)
                {
                    return null;
                }
            };
        }

        if (_items is null || createNewStorage)
        {
            _items = new();
        }
    }

    bool TryReplaceSameItem((object Id, TEntity Item) value)
    {
        var node = _items.First;
        while (node is not null && !_idEqualityComparer.Equals(node.Value.Id, value.Id))
        {
            node = node.Next;
        }
        if (node is null)
        {
            return false;
        }
        node.Value = value;
        return true;
    }

    void AddWhenNotFull((object Id, TEntity Item) value)
    {
        var node = _items.First;
        if (node is null)
        {
            _items.AddFirst(value);
            return;
        }

        if (ShouldAddBeforeNode(node, value))
        {
            _items.AddFirst(value);
            return;
        }
        while (node.Next is not null)
        {
            if (!SortingIsEnabled())
            {
                node = node.Next;
            }
            else
            {
                if (ShouldAddBeforeNode(node.Next, value))
                {
                    _items.AddAfter(node, value);
                    return;
                }
                node = node.Next;
            }
        }
        _items.AddAfter(node, value);
    }

    bool AddWhenFull((object Id, TEntity Item) value)
    {
        if (!SortingIsEnabled())
        {
            return false;
        }
        var node = _items.First!;
        if (ShouldAddBeforeNode(node, value))
        {
            _items.RemoveLast();
            _items.AddFirst(value);
            return true;
        }
        while (node.Next is not null)
        {
            if (ShouldAddBeforeNode(node.Next, value))
            {
                _items.RemoveLast();
                _items.AddAfter(node, value);
                return true;
            }
            node = node.Next;
        }

        return false;
    }

    bool ShouldAddBeforeNode(LinkedListNode<(object Id, TEntity Entity)> node, (object Id, TEntity Entity) value)
    {
        var sortingFieldX = _getSortingField(value.Entity);
        var sortingFieldY = _getSortingField(node.Value.Entity);
        var comparison = _sortingFieldComparer.Compare(sortingFieldX, sortingFieldY);
        comparison = _queryContext!.Sorting.Direction is SortDirection.Descending ? comparison * -1 : comparison;
        return comparison < 0;
    }

    bool SortingIsEnabled() => _queryContext?.Sorting != Sorting.None;
}
