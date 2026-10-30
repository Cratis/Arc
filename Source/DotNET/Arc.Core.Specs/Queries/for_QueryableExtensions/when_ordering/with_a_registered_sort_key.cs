// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class with_a_registered_sort_key : Specification
{
    record item(string Name);

    IQueryable _queryable;
    IQueryable _marker;
    fake_sort_key _fake;
    IQueryable _result;

    void Establish()
    {
        _queryable = new item[] { new("a") }.AsQueryable();
        _marker = new item[] { new("marker") }.AsQueryable();
        _fake = new(_marker);
        QueryableExtensions.RegisterSortKey(typeof(item), typeof(string), _fake);
    }

    void Because() => _result = _queryable.OrderBy("Name", SortDirection.Descending);

    [Fact] void should_return_what_the_registered_sort_key_returned() => _result.ShouldBeSame(_marker);
    [Fact] void should_pass_the_queryable() => _fake.Queryable.ShouldBeSame(_queryable);
    [Fact] void should_pass_the_resolved_property() => _fake.Property.ShouldEqual(typeof(item).GetProperty("Name"));
    [Fact] void should_pass_the_direction() => _fake.Direction.ShouldEqual(SortDirection.Descending);

    sealed class fake_sort_key(IQueryable marker) : QueryableSortKey
    {
        public IQueryable Queryable { get; private set; }

        public PropertyInfo Property { get; private set; }

        public SortDirection Direction { get; private set; }

        public override IQueryable OrderBy(IQueryable queryable, PropertyInfo property, SortDirection direction)
        {
            Queryable = queryable;
            Property = property;
            Direction = direction;
            return marker;
        }
    }
}
