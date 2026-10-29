// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_a_type_opt_in : given.a_caller_request
{
    void Establish() => _commandType = typeof(OptedInCommand);
    void Because() => Cap();

    [Fact] void should_allow_error() => _result.ShouldEqual(ValidationResultSeverity.Error);

    [AllowCallerValidationSeverity(ValidationResultSeverity.Error)]
    public record OptedInCommand;
}
