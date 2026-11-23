// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_custom_resolver_coverage_is_explicit : given.a_wiring_check
{
    Resolver _resolver;
    void Establish()
    {
        _resolver = new Resolver();
        Services.AddSingleton<IAsyncParameterDependencyResolver>(_resolver).AddScoped<IDependency, Dependency>();
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, new CommandWiringOptions { Rules = [new Rule()] }, [typeof(ValidCommand)]);
    [Fact] void should_accept_coverage_without_executing_the_resolver() => Problems.ShouldBeEmpty();
    [Fact] void should_not_call_the_covered_resolver() => _resolver.Calls.ShouldEqual(0);
}
