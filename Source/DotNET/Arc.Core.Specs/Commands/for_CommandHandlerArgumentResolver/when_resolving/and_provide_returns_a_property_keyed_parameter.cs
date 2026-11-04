// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_a_property_keyed_parameter : given.a_command_handler_argument_resolver
{
    public sealed class SampleModel;

    class Handler
    {
        public void Handle([LookUpBy("Id")] SampleModel model) { }
    }

    SampleModel _provided;
    CommandHandlerArgumentResolution _result;

    void Establish()
    {
        HandleHasParameters<Handler>();
        _provided = new SampleModel();
        ProvideReturns(_provided);
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_use_the_provided_value() => _result.Arguments[0].ShouldEqual(_provided);
}
