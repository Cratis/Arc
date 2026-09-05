// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_dependency_cannot_be_constructed : given.a_command_handler_argument_resolver
{
    class MissingDependency;

    class Dependency(MissingDependency missing)
    {
        public MissingDependency Missing => missing;
    }

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
            .AddTransient<Dependency>()
            .BuildServiceProvider();
        _serviceProvider = _provider;
    }

    async Task Because() => _exception = await Catch.Exception(async () => await Resolve());

    void Destroy() => _provider.Dispose();

    [Fact] void should_throw_cannot_resolve_dependency() => _exception.ShouldBeOfExactType<CannotResolveDependency>();
    [Fact] void should_name_the_dependency_that_failed() => _exception.Message.ShouldContain(typeof(Dependency).FullName!);
    [Fact] void should_preserve_the_underlying_failure() => _exception.InnerException.ShouldNotBeNull();
}
