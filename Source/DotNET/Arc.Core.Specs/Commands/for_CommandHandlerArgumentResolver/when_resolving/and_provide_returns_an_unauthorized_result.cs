// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_an_unauthorized_result : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(string value) { }
    }

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns(new AuthorizationResult(false, "Denied"));
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_short_circuit() => _result.IsShortCircuited.ShouldBeTrue();
    [Fact] void should_not_be_authorized() => _result.ControlResult.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_build_any_arguments() => _result.Arguments.ShouldBeEmpty();
}
