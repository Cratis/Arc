// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System.Linq.Expressions;
using System.Reflection;
using Cratis.Arc.Queries;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

public record OrderingItem(string Name, int Rank, OrderingStruct Mark);

public record struct OrderingStruct(int Value);

public abstract class an_ordering_without_dynamic_code : Specification
{
    protected OrderingItem[] _items;

    void Establish() => _items =
    [
        new("b", 2, new(2)),
        new("c", 3, new(3)),
        new("a", 1, new(1))
    ];

    protected IQueryable<OrderingItem> OrderBy(string property, SortDirection direction, IQueryable<OrderingItem>? source = null)
    {
        var parameter = Expression.Parameter(typeof(OrderingItem), "x");
        var access = Expression.Property(parameter, typeof(OrderingItem).GetProperty(property)!);
        return Microsoft.EntityFrameworkCore.DbSetObserveExtensions.OrderByWithoutDynamicCode(source ?? _items.AsQueryable(), access, parameter, direction);
    }
}
#endif
