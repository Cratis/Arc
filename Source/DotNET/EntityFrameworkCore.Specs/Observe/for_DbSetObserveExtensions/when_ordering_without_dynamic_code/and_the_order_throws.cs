// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System.Reflection;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_ordering_without_dynamic_code;

public class and_the_order_throws : an_ordering_without_dynamic_code
{
    InvalidOperationException _failure;
    IQueryable<OrderingItem> _source;
    Exception _exception;

    void Establish()
    {
        _failure = new InvalidOperationException("provider failure");
        _source = Substitute.For<IQueryable<OrderingItem>>();
        _source.Provider.Returns(_ => throw _failure);
    }

    void Because() => _exception = Catch.Exception(() => OrderBy(nameof(OrderingItem.Name), SortDirection.Ascending, _source));

    [Fact] void should_throw_a_target_invocation_exception() => _exception.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_carry_the_failure() => _exception.InnerException.ShouldEqual(_failure);
}
#endif
