// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_adding;

public class and_the_page_size_is_out_of_range : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
    {
        _set = new(QueryContextBuilder.New()
            .WithPageSize(0)
            .Build());
        _set.Add(new(Guid.NewGuid(), 1));
        _set.Add(new(Guid.NewGuid(), 2));
    });

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_clamp_the_page_size_to_a_single_item() => _set.Count().ShouldEqual(1);
}
