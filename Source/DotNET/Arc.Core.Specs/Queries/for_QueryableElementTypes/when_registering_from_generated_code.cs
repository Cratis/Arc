// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableElementTypes;

public class when_registering_from_generated_code : Specification
{
    record item(string Name);

    IQueryable _queryable;
    item[] _items = [new("c"), new("a"), new("b"), new("d")];
    int _count;
    string[] _skipped;
    string[] _taken;
    string[] _ordered;

    void Establish()
    {
        QueryableElementTypes.Register<item>();
        QueryableElementTypes.Register<item>();
        QueryableElementTypes.RegisterSortKey<item, string>();
        QueryableElementTypes.RegisterSortKey<item, string>();
        _queryable = _items.AsQueryable();
    }

    void Because()
    {
        _count = _queryable.Count();
        _skipped = [.. _queryable.Skip(1).Cast<item>().Select(_ => _.Name)];
        _taken = [.. _queryable.Take(2).Cast<item>().Select(_ => _.Name)];
        _ordered = [.. _queryable.OrderBy("Name").Cast<item>().Select(_ => _.Name)];
    }

    [Fact] void should_count_like_linq() => _count.ShouldEqual(_items.Length);
    [Fact] void should_skip_like_linq() => _skipped.ShouldEqual(_items.Skip(1).Select(_ => _.Name));
    [Fact] void should_take_like_linq() => _taken.ShouldEqual(_items.Take(2).Select(_ => _.Name));
    [Fact] void should_order_like_linq() => _ordered.ShouldEqual(_items.OrderBy(_ => _.Name).Select(_ => _.Name));
}
