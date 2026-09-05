// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_checking_if_a_mutable_dictionary_is_a_dictionary : Specification
{
    bool _result;

    void Because() => _result = typeof(Dictionary<string, object>).IsDictionary();

    [Fact] void should_be_a_dictionary() => _result.ShouldBeTrue();
}
