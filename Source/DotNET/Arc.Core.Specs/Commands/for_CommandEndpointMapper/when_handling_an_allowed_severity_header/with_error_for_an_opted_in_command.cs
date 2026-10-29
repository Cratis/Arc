// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_handling_an_allowed_severity_header;

public class with_error_for_an_opted_in_command : given.a_custom_pipeline
{
    void Establish() => _commandType = typeof(OptedInCommand);
    Task Because() => HandleEndpoints();

    [Fact] void should_pass_error_to_execute_and_validate() => _severities.ShouldEqual(new ValidationResultSeverity?[] { ValidationResultSeverity.Error, ValidationResultSeverity.Error });

    [AllowCallerValidationSeverity(ValidationResultSeverity.Error)]
    public record OptedInCommand;
}
