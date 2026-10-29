// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_handling_an_allowed_severity_header;

public class with_error_for_a_command_without_policy : given.a_custom_pipeline
{
    Task Because() => HandleEndpoints();

    [Fact] void should_cap_execute_and_validate_at_warning() => _severities.ShouldEqual(new ValidationResultSeverity?[] { ValidationResultSeverity.Warning, ValidationResultSeverity.Warning });
}
