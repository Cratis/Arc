// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_multiple_values_are_provided : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(string first, int second) { }
    }

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns("hello", 42);
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_not_short_circuit() => _result.IsShortCircuited.ShouldBeFalse();
    [Fact] void should_match_the_first_parameter_by_type() => _result.Arguments[0].ShouldEqual("hello");
    [Fact] void should_match_the_second_parameter_by_type() => _result.Arguments[1].ShouldEqual(42);
}
