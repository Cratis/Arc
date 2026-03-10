// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Arc.Queries;
using Cratis.Strings;

namespace MongoDB.Driver;

/// <summary>
/// Represents a set that is aware of <see cref="QueryContext"/>.
/// </summary>
/// <remarks>This list is not mutated in a thread-safe way.</remarks>
/// <typeparam name="TDocument">The type of the document.</typeparam>
internal sealed class QueryContextAwareSet<TDocument> : IEnumerable<TDocument>
{
    readonly IEqualityComparer _idEqualityComparer;
    readonly Func<TDocument, object> _getId;
    LinkedList<(object Id, TDocument Document)> _items = new();
    QueryContext? _queryContext;
    int? _maxSize;
    Func<TDocument, object?> _getSortingField = _ => null;
    IComparer _sortingFieldComparer = Comparer<object>.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryContextAwareSet{TDocument}"/> class.
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
        _getId = document =>
        {
            var id = idProperty.GetValue(document);
            ArgumentNullException.ThrowIfNull(id);
            return id;
        };
        Initialize(queryContext);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryContextAwareSet{TDocument}"/> class.
    /// </summary>
    /// <param name="queryContext">The query context.</param>
    /// <remarks>
    /// Primarily used for testing.
    /// </remarks>
    public QueryContextAwareSet(QueryContext queryContext) : this(queryContext, typeof(TDocument).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!)
    {
    }

    /// <summary>
    /// Adds item to the set.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>True if added new item or changed stored item, false if not.</returns>
    public bool Add(TDocument item)
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
    /// Initializes the set from the <see cref="IFindFluent{TDocument,TProjection}"/> query.
    /// </summary>
    /// <param name="query">The sorted query.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task InitializeWithQuery(IFindFluent<TDocument, TDocument> query) =>
        query.ForEachAsync(document => _items.AddLast((_getId(document), document)));

    /// <summary>
    /// Removes the document with the given id.
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
    /// Removes the document with the given id and adds the last document from the given query.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="query">The query.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<bool> RemoveAndAddLastInQuery(object id, IFindFluent<TDocument, TDocument> query)
    {
        var removed = Remove(id);
        if (_items.Count >= _maxSize || NotFilledUpPage())
        {
            return removed;
        }
        var countInQuery = (int)await query.CountDocumentsAsync();
        switch (countInQuery)
        {
            case 0:
                return removed;
            case >1:
                query = query.Skip(countInQuery - 1);
                break;
        }
        var document = await query.SingleAsync();
        _items.AddLast((_getId(document), document));
        return removed;
        bool NotFilledUpPage() => _items.Count < _maxSize - 1;
    }

    /// <inheritdoc/>
    public IEnumerator<TDocument> GetEnumerator()
    {
        return _items.Select(node => node.Document).GetEnumerator();
    }

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
            var sortingFieldProperty = typeof(TDocument).GetProperty(_queryContext.Sorting.Field.ToPascalCase(), BindingFlags.Instance | BindingFlags.Public) ?? throw new ArgumentException($"Sorting field could not be found on {typeof(TDocument)}", nameof(newQueryContext));
            _sortingFieldComparer = (typeof(Comparer<>)
                .MakeGenericType(sortingFieldProperty.PropertyType)
                .GetProperty(nameof(Comparer<object>.Default), BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)
                as IComparer)!;
            _getSortingField = document =>
            {
                try
                {
                    return sortingFieldProperty.GetValue(document);
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

    bool TryReplaceSameItem((object Id, TDocument Item) value)
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

    void AddWhenNotFull((object Id, TDocument Item) value)
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

    bool AddWhenFull((object Id, TDocument Item) value)
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

    bool ShouldAddBeforeNode(LinkedListNode<(object Id, TDocument Doucment)> node, (object Id, TDocument Document) value)
    {
        var sortingFieldX = _getSortingField(value.Document);
        var sortingFieldY = _getSortingField(node.Value.Doucment);
        var comparison = _sortingFieldComparer.Compare(sortingFieldX, sortingFieldY);
        comparison = _queryContext!.Sorting.Direction is Cratis.Arc.Queries.SortDirection.Descending ? comparison * -1 : comparison;
        return comparison < 0;
    }

    bool SortingIsEnabled() => _queryContext?.Sorting != Sorting.None;
}