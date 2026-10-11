// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValues;

public class when_converting_to_string : Specification
{
    object _values;
    string _result;

    void Establish()
    {
        var values = new GeneratedValues();
        values.Add("token", typeof(string), "sensitive-generated-value");
        _values = values;
    }

    void Because() => _result = _values.ToString()!;

    [Fact] void should_not_include_the_generated_value() => _result.ShouldNotContain("sensitive-generated-value");
}
