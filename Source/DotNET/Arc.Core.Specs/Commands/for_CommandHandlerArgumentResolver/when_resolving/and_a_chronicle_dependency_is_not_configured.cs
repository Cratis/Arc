// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.Specs.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_chronicle_dependency_is_not_configured : given.a_command_handler_argument_resolver
{
    class Handler
    {
        public void Handle(FakeEventLog eventLog) { }
    }

    Exception _exception;
    ServiceProvider _provider;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns();
        _provider = new ServiceCollection()
            .AddTransient<FakeEventLog>()
            .BuildServiceProvider();
        _serviceProvider = _provider;
    }

    async Task Because() => _exception = await Catch.Exception(async () => await Resolve());

    void Destroy() => _provider.Dispose();

    [Fact] void should_throw_cannot_resolve_dependency() => _exception.ShouldBeOfExactType<CannotResolveDependency>();
    [Fact] void should_hint_that_chronicle_is_not_configured() => _exception.Message.ShouldContain("WithChronicle");
    [Fact] void should_point_to_the_all_in_one_setup() => _exception.Message.ShouldContain("AddCratis");
}
