// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class by_a_nested_field_path : Specification
{
    record inner(string Name);

    record item(inner Inner);

    Exception _result;

    void Because() => _result = Catch.Exception(() => new item[] { new(new("a")) }.AsQueryable().OrderBy("Inner.Name"));

    [Fact] void should_throw_null_reference() => _result.ShouldBeOfExactType<NullReferenceException>();
}
