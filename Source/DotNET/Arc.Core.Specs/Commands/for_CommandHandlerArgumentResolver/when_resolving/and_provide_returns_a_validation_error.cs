// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_a_validation_error : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(string value) { }
    }

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns(ValidationResult.Error("Not allowed"));
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_short_circuit() => _result.IsShortCircuited.ShouldBeTrue();
    [Fact] void should_not_build_any_arguments() => _result.Arguments.ShouldBeEmpty();
    [Fact] void should_carry_the_validation_result() => _result.ControlResult.ValidationResults.ShouldNotBeEmpty();
}
