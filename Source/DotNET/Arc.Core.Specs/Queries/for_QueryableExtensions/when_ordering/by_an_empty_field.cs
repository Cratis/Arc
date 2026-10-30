// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class by_an_empty_field : Specification
{
    record item(string Name);

    Exception _result;

    void Because() => _result = Catch.Exception(() => new item[] { new("a") }.AsQueryable().OrderBy(string.Empty));

    [Fact] void should_throw_null_reference() => _result.ShouldBeOfExactType<NullReferenceException>();
}
