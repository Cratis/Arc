// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_a_single_value : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(string value) { }
    }

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns("the value");
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_not_short_circuit() => _result.IsShortCircuited.ShouldBeFalse();
    [Fact] void should_have_a_single_argument() => _result.Arguments.Count.ShouldEqual(1);
    [Fact] void should_pass_the_provided_value() => _result.Arguments[0].ShouldEqual("the value");
}
