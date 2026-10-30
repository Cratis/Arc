// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.Queries;

/// <summary>
/// Provides a set of methods for working with <see cref="IQueryable"/>.
/// </summary>
/// <remarks>
/// The element type of a non-generic <see cref="IQueryable"/> is only known at runtime. Each operation is therefore
/// performed through operations registered for the element type and, for sorting, the type of the sort property. The
/// Arc source generator registers these for model-bound read models. Under the JIT, types without a registration are
/// closed through reflection over generic methods on this type, which are looked up by name so the trimmer can see which
/// methods are needed. In ahead-of-time compiled code, where reflection cannot close generic methods, such types are not
/// supported.
/// </remarks>
public static class QueryableExtensions
{
    static readonly ConcurrentDictionary<Type, QueryableElementOperations> _operationsByElementType = new();
    static readonly ConcurrentDictionary<(Type Element, Type Key), QueryableSortKey> _sortKeys = new();

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <returns>The number of elements in the input sequence.</returns>
    /// <exception cref="MissingQueryableElementType">Thrown when the element type is not registered and dynamic code is not supported.</exception>
    public static int Count(this IQueryable queryable) => OperationsFor(queryable.ElementType).Count(queryable);

    /// <summary>
    /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
    /// </summary>
    /// <param name="queryable">An <see cref="IQueryable"/> to adorn.</param>
    /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    /// <exception cref="MissingQueryableElementType">Thrown when the element type is not registered and dynamic code is not supported.</exception>
    public static IQueryable Skip(this IQueryable queryable, int count) => OperationsFor(queryable.ElementType).Skip(queryable, count);

    /// <summary>
    /// Returns a specified number of contiguous elements from the start of a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    /// <exception cref="MissingQueryableElementType">Thrown when the element type is not registered and dynamic code is not supported.</exception>
    public static IQueryable Take(this IQueryable queryable, int count) => OperationsFor(queryable.ElementType).Take(queryable, count);

    /// <summary>
    /// Sorts the elements of a sequence in ascending or descending order according to a key.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="field">The name of the field to order on.</param>
    /// <param name="direction">Optional direction of sort. Defaults to ascending.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    /// <exception cref="MissingQueryableSortKey">Thrown when the property type is not registered and dynamic code is not supported.</exception>
    /// <exception cref="TargetInvocationException">Thrown when building the sort fails, with the failure as inner exception.</exception>
    public static IQueryable OrderBy(this IQueryable queryable, string field, SortDirection direction = SortDirection.Ascending)
    {
        var elementTypeProperty = queryable.ElementType.GetProperty(field)!;
        var keyType = elementTypeProperty.PropertyType;

        if (_sortKeys.TryGetValue((queryable.ElementType, keyType), out var sortKey))
        {
            try
            {
                return sortKey.OrderBy(queryable, elementTypeProperty, direction);
            }
            catch (Exception ex)
            {
                // Reproduces the exception that invoking the sort through reflection throws, so callers see the same type and message.
                throw new TargetInvocationException(ex);
            }
        }

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new MissingQueryableSortKey(queryable.ElementType, keyType);
        }

        return OrderByThroughReflection(queryable, elementTypeProperty, direction);
    }

    /// <summary>
    /// Sorts the elements of a sequence in descending order according to a key.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="field">The name of the field to order on.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    /// <exception cref="MissingQueryableSortKey">Thrown when the property type is not registered and dynamic code is not supported.</exception>
    public static IQueryable OrderByDescending(this IQueryable queryable, string field) =>
        queryable.OrderBy(field, SortDirection.Descending);

    /// <summary>
    /// Registers the operations of an element type. The first registration for an element type is kept.
    /// </summary>
    /// <param name="elementType">The element type of the queryable.</param>
    /// <param name="operations">The <see cref="QueryableElementOperations"/> for the element type.</param>
    internal static void Register(Type elementType, QueryableElementOperations operations) =>
        _operationsByElementType.TryAdd(elementType, operations);

    /// <summary>
    /// Registers sorting for an element type and a sort key type. The first registration is kept.
    /// </summary>
    /// <param name="elementType">The element type of the queryable.</param>
    /// <param name="keyType">The type of the property to sort on.</param>
    /// <param name="sortKey">The <see cref="QueryableSortKey"/> for the types.</param>
    internal static void RegisterSortKey(Type elementType, Type keyType, QueryableSortKey sortKey) =>
        _sortKeys.TryAdd((elementType, keyType), sortKey);

    /// <summary>
    /// Sorts a queryable on a property, closed over the element type and the property type.
    /// </summary>
    /// <typeparam name="TElement">The element type of the queryable.</typeparam>
    /// <typeparam name="TKey">The type of the property to sort on.</typeparam>
    /// <param name="queryable">The <see cref="IQueryable"/> to sort.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> of the property to sort on.</param>
    /// <param name="direction">The <see cref="SortDirection"/>.</param>
    /// <returns>An <see cref="IQueryable{T}"/> for continuation.</returns>
    internal static IQueryable<TElement> OrderByProperty<TElement, TKey>(IQueryable queryable, PropertyInfo property, SortDirection direction)
    {
        var parameter = Expression.Parameter(typeof(TElement), "x");
        var keySelector = Expression.Lambda<Func<TElement, TKey>>(Expression.Property(parameter, property), parameter);
        var typed = (IQueryable<TElement>)queryable;

        return direction == SortDirection.Ascending ? typed.OrderBy(keySelector) : typed.OrderByDescending(keySelector);
    }

    [RequiresDynamicCode("Closes the sort over an element type and a property type only known at runtime.")]
    static IQueryable OrderByThroughReflection(IQueryable queryable, PropertyInfo elementTypeProperty, SortDirection direction)
    {
        var orderBy = typeof(QueryableExtensions)
            .GetMethod(nameof(OrderByProperty), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(queryable.ElementType, elementTypeProperty.PropertyType);

        return (orderBy.Invoke(null, [queryable, elementTypeProperty, direction]) as IQueryable)!;
    }

    static QueryableElementOperations OperationsFor(Type elementType)
    {
        if (_operationsByElementType.TryGetValue(elementType, out var operations))
        {
            return operations;
        }

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new MissingQueryableElementType(elementType);
        }

        return _operationsByElementType.GetOrAdd(elementType, CreateThroughReflection);
    }

    [RequiresDynamicCode("Closes the queryable operations over an element type only known at runtime.")]
    static QueryableElementOperations CreateThroughReflection(Type elementType) =>
        (QueryableElementOperations)typeof(QueryableExtensions)
            .GetMethod(nameof(CreateOperations), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(elementType)
            .Invoke(null, null)!;

    static QueryableElementOperationsFor<TElement> CreateOperations<TElement>() => new();
}
