// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_parameter_is_not_provided : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(string value) { }
    }

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns();
        _serviceProvider.GetService(typeof(string)).Returns("from di");
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_fall_back_to_the_service_provider() => _result.Arguments[0].ShouldEqual("from di");
    [Fact] void should_resolve_the_parameter_from_di() => _serviceProvider.Received().GetService(typeof(string));
}
