// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_value_overrides_a_di_type : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(string value) { }
    }

    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns("from provide");
        _serviceProvider.GetService(typeof(string)).Returns("from di");
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_use_the_provided_value() => _result.Arguments[0].ShouldEqual("from provide");
    [Fact] void should_not_resolve_from_di() => _serviceProvider.DidNotReceive().GetService(typeof(string));
}
