// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_nullable_dependency_resolves_to_null : given.a_command_provide_invoker
{
    class Dependency;

    class Command
    {
        public bool Provide(Dependency? dependency) => dependency is null;
    }

    IReadOnlyList<object> _result;
    ServiceProvider _provider;

    void Establish()
    {
        _provider = new ServiceCollection()
            .AddScoped<Dependency>(_ => null!)
            .BuildServiceProvider();
        _serviceProvider = _provider;
    }

    async Task Because() => _result = await Invoke(new Command());

    void Destroy() => _provider.Dispose();

    [Fact] void should_pass_null_to_provide() => _result[0].ShouldEqual(true);
}
