// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_a_requested_severity_below_the_maximum : given.a_caller_request
{
    void Establish() => _requested = ValidationResultSeverity.Information;
    void Because() => Cap();

    [Fact] void should_keep_the_lower_request() => _result.ShouldEqual(ValidationResultSeverity.Information);
}
