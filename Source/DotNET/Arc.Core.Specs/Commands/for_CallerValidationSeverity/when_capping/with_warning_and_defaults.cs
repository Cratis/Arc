// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_warning_and_defaults : given.a_caller_request
{
    void Establish() => _requested = ValidationResultSeverity.Warning;
    void Because() => Cap();

    [Fact] void should_keep_warning() => _result.ShouldEqual(ValidationResultSeverity.Warning);
}
