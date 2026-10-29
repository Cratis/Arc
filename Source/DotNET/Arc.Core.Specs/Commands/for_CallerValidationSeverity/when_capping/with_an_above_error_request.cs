// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_an_above_error_request : given.a_caller_request
{
    void Establish() => _requested = CallerValidationSeverity.FromHeaders(new Dictionary<string, string> { ["X-Allowed-Severity"] = "999" });
    void Because() => Cap();

    [Fact] void should_ignore_the_header() => _result.ShouldBeNull();
}
