// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_a_lower_host_maximum : given.a_caller_request
{
    void Establish() => _options.MaximumCallerAllowedSeverity = ValidationResultSeverity.Unknown;
    void Because() => Cap();

    [Fact] void should_use_the_host_maximum() => _result.ShouldEqual(ValidationResultSeverity.Unknown);
}
