// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_non_nullable_dependency_resolves_to_null : given.a_command_provide_invoker
{
    class Dependency;

    class Command
    {
        public bool Provide(Dependency dependency) => dependency is not null;
    }

    Exception _exception;
    ServiceProvider _provider;

    void Establish()
    {
        _provider = new ServiceCollection()
            .AddScoped<Dependency>(_ => null!)
            .BuildServiceProvider();
        _serviceProvider = _provider;
    }

    async Task Because() => _exception = await Catch.Exception(async () => await Invoke(new Command()));

    void Destroy() => _provider.Dispose();

    [Fact] void should_throw_cannot_resolve_command_dependency() => _exception.ShouldBeOfExactType<CannotResolveCommandDependency>();
    [Fact] void should_explain_that_dependency_could_not_be_resolved_or_resolved_to_null() => _exception.Message.ShouldContain("could not be resolved or resolved to null");
}
