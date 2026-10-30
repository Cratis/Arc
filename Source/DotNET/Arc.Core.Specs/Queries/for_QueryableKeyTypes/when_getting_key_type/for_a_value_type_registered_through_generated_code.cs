// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableKeyTypes.when_getting_key_type;

public class for_a_value_type_registered_through_generated_code : Specification
{
    record struct SpecStruct(int Value);

    record SpecElement(SpecStruct Key);

    QueryableKeyType _keyType;

    void Establish() => QueryableElementTypes.RegisterSortKey<SpecElement, SpecStruct>();

    void Because() => _keyType = QueryableKeyTypes.For(typeof(SpecElement), typeof(SpecStruct));

    [Fact] void should_use_the_default_equality_comparer() => _keyType.EqualityComparer.ShouldEqual(EqualityComparer<SpecStruct>.Default);
    [Fact] void should_use_the_default_comparer() => _keyType.Comparer.ShouldEqual(Comparer<SpecStruct>.Default);
}
