// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValue;

public class when_converting_to_string : Specification
{
    GeneratedValue _value;
    string _result;

    void Establish() => _value = new(0, "token", typeof(string), "sensitive-generated-value");

    void Because() => _result = _value.ToString();

    [Fact] void should_not_include_the_generated_value() => _result.ShouldNotContain((string)_value.Value);
    [Fact] void should_include_only_the_metadata() => _result.ShouldEqual("GeneratedValue { Order = 0, Name = token, Type = System.String }");
}
