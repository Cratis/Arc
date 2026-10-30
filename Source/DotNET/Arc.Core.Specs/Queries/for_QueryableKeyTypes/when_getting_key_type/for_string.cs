// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0024 // The default comparers are exactly what is being asserted.

namespace Cratis.Arc.Queries.for_QueryableKeyTypes.when_getting_key_type;

public class for_string : Specification
{
    QueryableKeyType _keyType;

    void Because() => _keyType = QueryableKeyTypes.For(typeof(object), typeof(string));

    [Fact] void should_use_the_default_equality_comparer() => _keyType.EqualityComparer.ShouldEqual(EqualityComparer<string>.Default);
    [Fact] void should_use_the_default_comparer() => _keyType.Comparer.ShouldEqual(Comparer<string>.Default);
}
