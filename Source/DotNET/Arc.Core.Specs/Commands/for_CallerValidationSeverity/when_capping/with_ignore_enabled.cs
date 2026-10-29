// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_ignore_enabled : given.a_caller_request
{
    void Establish()
    {
        _commandType = typeof(OptedInCommand);
        _options.IgnoreCallerAllowedSeverity = true;
    }
    void Because() => Cap();

    [Fact] void should_ignore_the_request_even_with_an_opt_in() => _result.ShouldBeNull();

    [AllowCallerValidationSeverity(ValidationResultSeverity.Error)]
    public record OptedInCommand;
}
