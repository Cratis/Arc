// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_reading_headers;

public class with_a_name : Specification
{
    Validation.ValidationResultSeverity? _result;
    void Because() => _result = CallerValidationSeverity.FromHeaders(new Dictionary<string, string> { ["X-Allowed-Severity"] = "Warning" });

    [Fact] void should_ignore_the_name() => _result.ShouldBeNull();
}
