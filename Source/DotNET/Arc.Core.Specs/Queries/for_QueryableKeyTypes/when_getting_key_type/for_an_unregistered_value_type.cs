// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableKeyTypes.when_getting_key_type;

public class for_an_unregistered_value_type : Specification
{
    record struct UnregisteredStruct(int Value);

    record SpecElement(UnregisteredStruct Key);

    Exception _exception;

    void Because() => _exception = Catch.Exception(() => QueryableKeyTypes.For(typeof(SpecElement), typeof(UnregisteredStruct)));

    [Fact] void should_throw_missing_queryable_sort_key() => _exception.ShouldBeOfExactType<MissingQueryableSortKey>();
    [Fact] void should_name_the_element_type() => _exception.Message.ShouldContain(typeof(SpecElement).ToString());
    [Fact] void should_name_the_key_type() => _exception.Message.ShouldContain(typeof(UnregisteredStruct).ToString());
}
