// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_a_host_maximum_of_error : given.a_caller_request
{
    void Establish() => _options.MaximumCallerAllowedSeverity = ValidationResultSeverity.Error;
    void Because() => Cap();

    [Fact] void should_allow_error() => _result.ShouldEqual(ValidationResultSeverity.Error);
}
