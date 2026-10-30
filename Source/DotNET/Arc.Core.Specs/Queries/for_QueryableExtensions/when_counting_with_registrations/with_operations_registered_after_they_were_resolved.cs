// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_counting_with_registrations;

public class with_operations_registered_after_they_were_resolved : Specification
{
    record item(int Value);

    IQueryable _queryable;
    int _result;

    void Establish()
    {
        _queryable = new item[] { new(1), new(2), new(3) }.AsQueryable();
        _queryable.Count();
        QueryableExtensions.Register(typeof(item), new fake_operations(42));
    }

    void Because() => _result = _queryable.Count();

    [Fact] void should_keep_the_first_operations() => _result.ShouldEqual(3);

    sealed class fake_operations(int count) : QueryableElementOperations
    {
        public override int Count(IQueryable queryable) => count;

        public override IQueryable Skip(IQueryable queryable, int count) => queryable;

        public override IQueryable Take(IQueryable queryable, int count) => queryable;
    }
}
