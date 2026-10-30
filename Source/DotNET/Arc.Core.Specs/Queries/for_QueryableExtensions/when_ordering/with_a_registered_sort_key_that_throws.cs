// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class with_a_registered_sort_key_that_throws : Specification
{
    record item(string Name);

    sort_failed _failure;
    Exception _result;

    void Establish()
    {
        _failure = new();
        QueryableExtensions.RegisterSortKey(typeof(item), typeof(string), new throwing_sort_key(_failure));
    }

    void Because() => _result = Catch.Exception(() => new item[] { new("a") }.AsQueryable().OrderBy("Name"));

    [Fact] void should_throw_target_invocation() => _result.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_wrap_the_failure() => _result.InnerException.ShouldBeSame(_failure);

    sealed class sort_failed : Exception;

    sealed class throwing_sort_key(Exception failure) : QueryableSortKey
    {
        public override IQueryable OrderBy(IQueryable queryable, PropertyInfo property, SortDirection direction) => throw failure;
    }
}
