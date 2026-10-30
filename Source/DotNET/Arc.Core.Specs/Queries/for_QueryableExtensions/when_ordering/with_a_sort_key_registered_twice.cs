// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class with_a_sort_key_registered_twice : Specification
{
    record item(string Name);

    IQueryable _first;
    IQueryable _result;

    void Establish()
    {
        _first = new item[] { new("first") }.AsQueryable();
        QueryableExtensions.RegisterSortKey(typeof(item), typeof(string), new fake_sort_key(_first));
        QueryableExtensions.RegisterSortKey(typeof(item), typeof(string), new fake_sort_key(new item[] { new("second") }.AsQueryable()));
    }

    void Because() => _result = new item[] { new("a") }.AsQueryable().OrderBy("Name");

    [Fact] void should_use_the_first_registration() => _result.ShouldBeSame(_first);

    sealed class fake_sort_key(IQueryable marker) : QueryableSortKey
    {
        public override IQueryable OrderBy(IQueryable queryable, PropertyInfo property, SortDirection direction) => marker;
    }
}
