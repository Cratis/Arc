// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_the_protected_provider_refuses_a_dependency : given.a_wiring_check
{
    ICommandProtectedDecisionSupport _support;

    void Establish()
    {
        _support = Substitute.For<ICommandProtectedDecisionSupport>();
        _support.When(_ => _.ValidateCommandDependency(typeof(IDependency), Arg.Any<object>())).Do(_ => throw new InvalidOperationException("Refused dependency"));
        Services.AddSingleton(_support).AddScoped<IDependency, Dependency>();
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ProtectedCommand)]);
    [Fact] void should_report_activation_failure() => Problems.Single().Category.ShouldEqual("activation");
    [Fact] void should_apply_the_protected_support() => _support.Received(1).ValidateCommandDependency(typeof(IDependency), Arg.Any<object>());
}
