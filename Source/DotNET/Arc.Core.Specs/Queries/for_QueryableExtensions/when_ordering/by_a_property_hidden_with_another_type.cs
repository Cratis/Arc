// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class by_a_property_hidden_with_another_type : Specification
{
    class base_item
    {
        public object Value { get; init; } = string.Empty;
    }

    class item : base_item
    {
        public new string Value { get; init; } = string.Empty;
    }

    Exception _result;

    void Because() => _result = Catch.Exception(() => new item[] { new() }.AsQueryable().OrderBy("Value"));

    [Fact] void should_throw_ambiguous_match() => _result.ShouldBeOfExactType<AmbiguousMatchException>();
}
