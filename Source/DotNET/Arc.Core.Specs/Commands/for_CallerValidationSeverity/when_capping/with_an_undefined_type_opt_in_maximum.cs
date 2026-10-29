// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_an_undefined_type_opt_in_maximum : given.a_caller_request
{
    Exception _exception;
    void Establish() => _commandType = typeof(UndefinedCommand);
    void Because() => _exception = Catch.Exception(Cap);

    [Fact] void should_reject_the_invalid_policy() => _exception.ShouldBeOfExactType<InvalidCommandValidationSeverity>();

    [AllowCallerValidationSeverity((ValidationResultSeverity)999)]
    public record UndefinedCommand;
}
