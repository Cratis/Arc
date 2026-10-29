// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_error_and_default_options : given.a_caller_request
{
    void Because() => Cap();

    [Fact] void should_cap_at_warning() => _result.ShouldEqual(ValidationResultSeverity.Warning);
}
