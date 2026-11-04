// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_returns_a_caller_keyed_parameter : given.a_command_handler_argument_resolver
{
    public sealed class SampleModel;

    public sealed class SampleKey : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new NotSupportedException();
    }

    class Handler
    {
        public void Handle([LookUpBy<SampleKey>] SampleModel model) { }
    }

    Exception _exception;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns(new SampleModel());
    }

    async Task Because() => _exception = await Catch.Exception(async () => await Resolve());

    [Fact] void should_refuse_the_declaration() => _exception.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_name_the_provider() => _exception.Message.ShouldContain(nameof(SampleKey));
    [Fact] void should_mention_provide() => _exception.Message.ShouldContain("Provide");
    [Fact] void should_not_resolve_from_di() => _serviceProvider.DidNotReceive().GetService(typeof(SampleModel));
}
