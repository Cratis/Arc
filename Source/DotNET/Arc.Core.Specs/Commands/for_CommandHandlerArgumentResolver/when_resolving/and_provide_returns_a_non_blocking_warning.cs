// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_a_non_blocking_warning : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(string value) { }
    }

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns(ValidationResult.Warning("Heads up"), "the value");
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_not_short_circuit() => _result.IsShortCircuited.ShouldBeFalse();
    [Fact] void should_still_build_arguments() => _result.Arguments[0].ShouldEqual("the value");
}
