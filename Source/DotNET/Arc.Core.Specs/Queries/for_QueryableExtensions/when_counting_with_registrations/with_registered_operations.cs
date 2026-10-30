// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_counting_with_registrations;

public class with_registered_operations : Specification
{
    record registered_item(int Value);

    int _result;

    void Establish() => QueryableExtensions.Register(typeof(registered_item), new fake_operations(42));

    void Because() => _result = new registered_item[] { new(1), new(2) }.AsQueryable().Count();

    [Fact] void should_use_the_registered_operations() => _result.ShouldEqual(42);

    sealed class fake_operations(int count) : QueryableElementOperations
    {
        public override int Count(IQueryable queryable) => count;

        public override IQueryable Skip(IQueryable queryable, int count) => queryable;

        public override IQueryable Take(IQueryable queryable, int count) => queryable;
    }
}
