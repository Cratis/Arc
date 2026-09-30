// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Provides a set of methods for working with <see cref="IQueryable"/>.
/// </summary>
/// <remarks>
/// The element type of a non-generic <see cref="IQueryable"/> is only known at runtime. Each operation is therefore
/// performed by a generic method closed over the element type, which calls the typed <see cref="Queryable"/> operator
/// directly. The generic methods are looked up by name on this type, so the trimmer can see which methods are needed.
/// </remarks>
public static class QueryableExtensions
{
    static readonly ConcurrentDictionary<Type, ElementOperations> _operationsByElementType = new();

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <returns>The number of elements in the input sequence.</returns>
    public static int Count(this IQueryable queryable) => OperationsFor(queryable.ElementType).Count(queryable);

    /// <summary>
    /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
    /// </summary>
    /// <param name="queryable">An <see cref="IQueryable"/> to adorn.</param>
    /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public static IQueryable Skip(this IQueryable queryable, int count) => OperationsFor(queryable.ElementType).Skip(queryable, count);

    /// <summary>
    /// Returns a specified number of contiguous elements from the start of a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public static IQueryable Take(this IQueryable queryable, int count) => OperationsFor(queryable.ElementType).Take(queryable, count);

    /// <summary>
    /// Sorts the elements of a sequence in ascending or descending order according to a key.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="field">The name of the field to order on.</param>
    /// <param name="direction">Optional direction of sort. Defaults to ascending.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public static IQueryable OrderBy(this IQueryable queryable, string field, SortDirection direction = SortDirection.Ascending)
    {
        var elementTypeProperty = queryable.ElementType.GetProperty(field)!;
        var orderBy = typeof(QueryableExtensions)
            .GetMethod(nameof(OrderByProperty), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(queryable.ElementType, elementTypeProperty.PropertyType);

        return (orderBy.Invoke(null, [queryable, elementTypeProperty, direction]) as IQueryable)!;
    }

    /// <summary>
    /// Sorts the elements of a sequence in descending order according to a key.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="field">The name of the field to order on.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public static IQueryable OrderByDescending(this IQueryable queryable, string field) =>
        queryable.OrderBy(field, SortDirection.Descending);

    static ElementOperations OperationsFor(Type elementType) =>
        _operationsByElementType.GetOrAdd(
            elementType,
            static type => (ElementOperations)typeof(QueryableExtensions)
                .GetMethod(nameof(CreateOperations), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, null)!);

    static ElementOperations<TElement> CreateOperations<TElement>() => new();

    static IQueryable<TElement> OrderByProperty<TElement, TKey>(IQueryable queryable, PropertyInfo property, SortDirection direction)
    {
        var parameter = Expression.Parameter(typeof(TElement), "x");
        var keySelector = Expression.Lambda<Func<TElement, TKey>>(Expression.Property(parameter, property), parameter);
        var typed = (IQueryable<TElement>)queryable;

        return direction == SortDirection.Ascending ? typed.OrderBy(keySelector) : typed.OrderByDescending(keySelector);
    }

    abstract class ElementOperations
    {
        public abstract int Count(IQueryable queryable);
        public abstract IQueryable Skip(IQueryable queryable, int count);
        public abstract IQueryable Take(IQueryable queryable, int count);
    }

    sealed class ElementOperations<TElement> : ElementOperations
    {
        public override int Count(IQueryable queryable) => Queryable.Count((IQueryable<TElement>)queryable);
        public override IQueryable Skip(IQueryable queryable, int count) => Queryable.Skip((IQueryable<TElement>)queryable, count);
        public override IQueryable Take(IQueryable queryable, int count) => Queryable.Take((IQueryable<TElement>)queryable, count);
    }
}
