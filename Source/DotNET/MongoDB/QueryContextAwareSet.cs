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
    bool _hasSortingField;

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
    /// Whether the set currently holds a document with the given id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>True when the document is in the set.</returns>
    /// <remarks>
    /// Tells an item entering the observed result set apart from one that was already in it, which
    /// is what keeps <see cref="QueryContext.TotalItems"/> honest when a document changes in a way
    /// that moves it across the observed filter.
    /// </remarks>
    public bool Contains(object id) => _items.Any(node => _idEqualityComparer.Equals(node.Id, id));

    /// <summary>
    /// Replaces the content of the set with a freshly read page, reporting how the page changed.
    /// </summary>
    /// <param name="page">The documents now on the page, in the order the query returned them.</param>
    /// <param name="hasSameContent">Decides whether two versions of a document with the same id carry the same content.</param>
    /// <returns>
    /// <see langword="null"/> when the page holds the same documents, in the same order, with the same content;
    /// otherwise the documents added, removed and replaced by id. The list is empty when only the order changed.
    /// </returns>
    /// <remarks>
    /// A paged observation cannot be maintained one change at a time: the set only holds the current page, so it
    /// cannot tell whether a changed document belongs before, on or after it. Re-reading the page and comparing is
    /// what keeps the page, its order and its total honest.
    /// </remarks>
    public IReadOnlyList<CollectionChange>? ReplaceWith(IEnumerable<TDocument> page, Func<TDocument, TDocument, bool> hasSameContent)
    {
        var previous = _items.ToList();
        var current = page.Select(document => (Id: _getId(document), Document: document)).ToList();
        var comparer = new IdEqualityComparer(_idEqualityComparer);
        var previousById = previous.ToDictionary(item => item.Id, item => item.Document, comparer);
        var currentIds = current.Select(item => item.Id).ToHashSet(comparer);

        var changes = new List<CollectionChange>();
        changes.AddRange(previous
            .Where(item => !currentIds.Contains(item.Id))
            .Select(item => new CollectionChange(CollectionChangeKind.Removed, item.Id)));
        foreach (var (id, document) in current)
        {
            if (!previousById.TryGetValue(id, out var previousDocument))
            {
                changes.Add(new(CollectionChangeKind.Added, id));
            }
            else if (!hasSameContent(previousDocument, document))
            {
                changes.Add(new(CollectionChangeKind.Replaced, id));
            }
        }

        var orderChanged = !previous.Select(item => item.Id).SequenceEqual(current.Select(item => item.Id), comparer);
        _items = new(current);
        return changes.Count > 0 || orderChanged ? changes : null;
    }

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
            // Clamp a non-positive (out-of-range) page size to a minimum of one item rather than throwing: a
            // malformed or hostile page size must degrade to a valid page instead of surfacing a server error.
            _maxSize = Math.Max(1, _queryContext.Paging.Size);
        }

        var createNewStorage = oldQueryContext?.Paging.IsPaged == true && _maxSize < oldQueryContext.Paging.Size;
        if (oldQueryContext is not null && oldQueryContext.Sorting != Sorting.None &&
            (newQueryContext.Sorting.Direction != oldQueryContext.Sorting.Direction || newQueryContext.Sorting.Field != oldQueryContext.Sorting.Field))
        {
            createNewStorage = true;
        }

        _sortingFieldComparer = Comparer<object>.Default;
        _hasSortingField = false;

        if (SortingIsEnabled())
        {
            // An unknown sort field is ignored rather than throwing: throwing would surface as a server error and
            // leak the read model type name for hostile or malformed input. The set keeps insertion order instead.
            var sortingFieldProperty = typeof(TDocument).GetProperty(_queryContext.Sorting.Field.Value.ToPascalCase(), BindingFlags.Instance | BindingFlags.Public);
            if (sortingFieldProperty is not null)
            {
                _hasSortingField = true;
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

        // Ties are broken on the id ascending, whatever the direction, which is the secondary sort the server applies.
        // Without it two documents sharing a sort value land in arrival order here and in an arbitrary order there.
        if (comparison == 0 && _hasSortingField)
        {
            comparison = DocumentIdComparer.Instance.Compare(value.Id, node.Value.Id);
        }

        return comparison < 0;
    }

    bool SortingIsEnabled() => _queryContext?.Sorting != Sorting.None;

    sealed class IdEqualityComparer(IEqualityComparer inner) : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => inner.Equals(x, y);

        public int GetHashCode(object obj) => inner.GetHashCode(obj);
    }
}