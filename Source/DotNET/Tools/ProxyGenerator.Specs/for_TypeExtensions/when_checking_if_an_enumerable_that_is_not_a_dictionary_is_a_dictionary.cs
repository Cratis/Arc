// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_checking_if_an_enumerable_that_is_not_a_dictionary_is_a_dictionary : Specification
{
    bool _result;

    void Because() => _result = typeof(IReadOnlyList<string>).IsDictionary();

    [Fact] void should_not_be_a_dictionary() => _result.ShouldBeFalse();
}
