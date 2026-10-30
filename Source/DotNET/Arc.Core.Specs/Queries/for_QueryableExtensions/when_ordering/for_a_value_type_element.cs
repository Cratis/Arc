// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class for_a_value_type_element : Specification
{
    record struct item(string Name);

    IQueryable _result;

    void Because() => _result = new item[] { new("b"), new("a"), new("c") }.AsQueryable().OrderBy("Name");

    [Fact] void should_be_ordered() => _result.Cast<item>().Select(_ => _.Name).ShouldEqual(["a", "b", "c"]);
}
