// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_enumerable_of_primitive_or_concept;

public class with_IEnumerable_of_string : Specification
{
    bool _result;

    void Because() => _result = typeof(IEnumerable<string>).IsEnumerableOfPrimitiveOrConcept();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
