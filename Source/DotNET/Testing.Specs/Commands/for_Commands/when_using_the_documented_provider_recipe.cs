// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_using_the_documented_provider_recipe : Specification
{
    Exception _error;
    ServiceProvider _root;
    IServiceScope _scope;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(Cratis.Arc.Validation.IDiscoverableValidator<>)).Returns([typeof(given.a_wiring_check.Validator)]);
        _root = new ServiceCollection()
            .AddSingleton(types)
            .AddScoped<given.a_wiring_check.IDependency, given.a_wiring_check.Dependency>()
            .BuildServiceProvider();
        _scope = _root.CreateScope();
    }

    void Because() => _error = Catch.Exception(() => CommandWiring.ShouldBeExecutableOf(_scope.ServiceProvider, typeof(given.a_wiring_check.ValidCommand)));
    void Destroy()
    {
        _scope.Dispose();
        _root.Dispose();
    }

    [Fact] void should_construct_dependencies_and_validator_without_a_host() => _error.ShouldBeNull();
}
