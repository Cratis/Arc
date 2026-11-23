// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_custom_resolver_is_not_covered : given.a_wiring_check
{
    IAsyncParameterDependencyResolver _resolver;

    void Establish()
    {
        _resolver = Substitute.For<IAsyncParameterDependencyResolver>();
        Services.AddSingleton(_resolver);
        Services.AddScoped<IDependency, Dependency>();
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ValidCommand), typeof(NullableCommand)]);
    [Fact] void should_report_one_provider_level_problem() => Problems.Single().CommandType.ShouldBeNull();
    [Fact] void should_report_unsupported_evidence() => Problems.Single().Category.ShouldEqual("unsupported");
    [Fact] void should_not_call_the_resolver() => _resolver.ReceivedCalls().ShouldBeEmpty();
}
