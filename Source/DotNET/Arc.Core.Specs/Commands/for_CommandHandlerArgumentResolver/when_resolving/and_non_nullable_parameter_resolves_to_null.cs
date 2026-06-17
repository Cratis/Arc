// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_non_nullable_parameter_resolves_to_null : given.a_command_handler_argument_resolver
{
    class Dependency;

    class Handler
    {
        public void Handle(Dependency dependency) { }
    }

    Exception _exception;
    ServiceProvider _provider;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns();
        _provider = new ServiceCollection()
            .AddScoped<Dependency>(_ => null!)
            .BuildServiceProvider();
        _serviceProvider = _provider;
    }

    async Task Because() => _exception = await Catch.Exception(async () => await Resolve());

    void Destroy() => _provider.Dispose();

    [Fact] void should_throw_cannot_resolve_command_dependency() => _exception.ShouldBeOfExactType<CannotResolveCommandDependency>();
    [Fact] void should_explain_that_dependency_could_not_be_resolved_or_resolved_to_null() => _exception.Message.ShouldContain("could not be resolved or resolved to null");
}
