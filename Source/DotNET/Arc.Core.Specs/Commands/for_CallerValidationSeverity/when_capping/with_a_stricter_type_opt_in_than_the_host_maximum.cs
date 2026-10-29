// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_a_stricter_type_opt_in_than_the_host_maximum : given.a_caller_request
{
    void Establish()
    {
        _commandType = typeof(StricterCommand);
        _options.MaximumCallerAllowedSeverity = ValidationResultSeverity.Error;
    }
    void Because() => Cap();

    [Fact] void should_use_the_type_maximum() => _result.ShouldEqual(ValidationResultSeverity.Information);

    [AllowCallerValidationSeverity(ValidationResultSeverity.Information)]
    public record StricterCommand;
}
