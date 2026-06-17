// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_nullable_parameter_resolves_to_an_instance : given.a_command_handler_argument_resolver
{
    class Dependency;

    class Handler
    {
        public void Handle(Dependency? dependency) { }
    }

    CommandHandlerArgumentResolution _result;
    Dependency _dependency;
    ServiceProvider _provider;

    void Establish()
    {
        _dependency = new();
        HandleHasParameters<Handler>();
        ProvideReturns();
        _provider = new ServiceCollection()
            .AddScoped(_ => _dependency)
            .BuildServiceProvider();
        _serviceProvider = _provider;
    }

    async Task Because() => _result = await Resolve();

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_instance_for_the_parameter() => _result.Arguments[0].ShouldEqual(_dependency);
}
