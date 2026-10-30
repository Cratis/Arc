// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableKeyTypes.when_getting_key_type;

public class for_a_primitive : Specification
{
    QueryableKeyType _keyType;

    void Because() => _keyType = QueryableKeyTypes.For(typeof(object), typeof(int));

    [Fact] void should_use_the_default_equality_comparer() => _keyType.EqualityComparer.ShouldEqual(EqualityComparer<int>.Default);
    [Fact] void should_use_the_default_comparer() => _keyType.Comparer.ShouldEqual(Comparer<int>.Default);
}
