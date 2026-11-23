// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_protected_validator_requires_attestation : given.a_wiring_check
{
    void Establish()
    {
        Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ProtectedValidator)]);
        Services.AddSingleton(Substitute.For<ICommandProtectedDecisionSupport>()).AddScoped<IDependency, Dependency>();
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ProtectedCommand)]);
    [Fact] void should_report_unsupported_instead_of_constructing_without_attestation() => Problems.Single().Category.ShouldEqual("unsupported");
}
