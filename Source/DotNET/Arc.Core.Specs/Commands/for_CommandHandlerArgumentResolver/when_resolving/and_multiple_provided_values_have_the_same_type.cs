// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_multiple_provided_values_have_the_same_type : given.a_command_handler_argument_resolver
{
    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns("first", "second");
        _serviceProvider.GetService(typeof(string)).Returns("fallback");
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_consume_each_provided_value_once_in_order() => _result.Arguments.ShouldEqual("first", "second", "fallback");
    [Fact] void should_resolve_only_the_remaining_parameter_from_services() => _serviceProvider.Received(1).GetService(typeof(string));

    class Handler
    {
        public void Handle(string first, string second, string third) { }
    }
}
