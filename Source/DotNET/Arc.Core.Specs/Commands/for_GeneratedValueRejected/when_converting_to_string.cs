// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValueRejected;

public class when_converting_to_string : Specification
{
    GeneratedValueRejected _error;
    string _result;

    void Establish() => _error = new(typeof(when_converting_to_string), "token", typeof(string), ["Rejected sensitive-generated-value"]);

    void Because() => _result = _error.ToString();

    [Fact] void should_not_include_sensitive_validator_failures() => _result.ShouldNotContain("sensitive-generated-value");
}
