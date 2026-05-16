// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Provides a set of methods for working with <see cref="IQueryable"/>.
/// </summary>
public static class QueryableExtensions
{
    static readonly MethodInfo _countMethod;
    static readonly MethodInfo _skipMethod;
    static readonly MethodInfo _takeMethod;
    static readonly MethodInfo _orderByMethod;
    static readonly MethodInfo _orderByDescendingMethod;

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Queryable static methods are framework types preserved by the runtime. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    static QueryableExtensions()
    {
        var queryableMethods = typeof(Queryable).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static);

        _countMethod = queryableMethods.First(m => m.Name == nameof(Queryable.Count) && m.GetParameters().Length == 1);
        _skipMethod = queryableMethods.First(m => m.Name == nameof(Queryable.Skip));
        _takeMethod = queryableMethods.First(m => m.Name == nameof(Queryable.Take));
        _orderByMethod = queryableMethods.First(
            m => m.Name == nameof(Queryable.OrderBy) &&
            m.GetParameters().Length == 2 &&
            m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));
        _orderByDescendingMethod = queryableMethods.First(
            m => m.Name == nameof(Queryable.OrderByDescending) &&
            m.GetParameters().Length == 2 &&
            m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));
    }

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <returns>The number of elements in the input sequence.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "queryable.ElementType is preserved by the type system since queries return strongly-typed IQueryable<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Runtime MakeGenericMethod for Queryable.Count<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    public static int Count(this IQueryable queryable)
    {
        var genericMethod = _countMethod.MakeGenericMethod([queryable.ElementType]);
        var countExpression = Expression.Call(null, genericMethod, queryable.Expression);
        return queryable.Provider.Execute<int>(countExpression);
    }

    /// <summary>
    /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
    /// </summary>
    /// <param name="queryable">An <see cref="IQueryable"/> to adorn.</param>
    /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "queryable.ElementType is preserved by the type system since queries return strongly-typed IQueryable<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Runtime MakeGenericMethod for Queryable.Skip<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    public static IQueryable Skip(this IQueryable queryable, int count)
    {
        var genericMethod = _skipMethod.MakeGenericMethod([queryable.ElementType]);
        return (genericMethod.Invoke(null, [queryable, count]) as IQueryable)!;
    }

    /// <summary>
    /// Returns a specified number of contiguous elements from the start of a sequence.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "queryable.ElementType is preserved by the type system since queries return strongly-typed IQueryable<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Runtime MakeGenericMethod for Queryable.Take<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    public static IQueryable Take(this IQueryable queryable, int count)
    {
        var genericMethod = _takeMethod.MakeGenericMethod([queryable.ElementType]);
        return (genericMethod.Invoke(null, [queryable, count]) as IQueryable)!;
    }

    /// <summary>
    /// Sorts the elements of a sequence in ascending or descending order according to a key.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="field">The name of the field to order on.</param>
    /// <param name="direction">Optional direction of sort. Defaults to ascending.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Expression.Property(string) is used with a runtime field name from the query sorting API. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "queryable.ElementType is preserved by the type system since queries return strongly-typed IQueryable<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "queryable.ElementType is preserved by the type system since queries return strongly-typed IQueryable<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Runtime MakeGenericMethod for Queryable.OrderBy<T>/OrderByDescending<T>. Source-generated LINQ dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    public static IQueryable OrderBy(this IQueryable queryable, string field, SortDirection direction = SortDirection.Ascending)
    {
        var orderMethod = direction == SortDirection.Ascending ? _orderByMethod : _orderByDescendingMethod;
        var elementTypeProperty = queryable.ElementType.GetProperty(field);
        orderMethod = orderMethod.MakeGenericMethod(queryable.ElementType, elementTypeProperty!.PropertyType);

        var parameter = Expression.Parameter(queryable.ElementType, "x");
        var property = Expression.Property(parameter, field);
        var lambda = Expression.Lambda(property, parameter);

        return (orderMethod.Invoke(null, [queryable, lambda]) as IQueryable)!;
    }

    /// <summary>
    /// Sorts the elements of a sequence in descending order according to a key.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to adorn.</param>
    /// <param name="field">The name of the field to order on.</param>
    /// <returns>An <see cref="IQueryable"/> for continuation.</returns>
    public static IQueryable OrderByDescending(this IQueryable queryable, string field) =>
        queryable.OrderBy(field, SortDirection.Descending);
}
