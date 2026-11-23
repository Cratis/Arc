// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_validator_constructor_throws : given.a_wiring_check
{
    void Establish()
    {
        Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ThrowingValidator)]);
        Services.AddScoped<IDependency, Dependency>();
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ThrowingCommand)]);
    [Fact] void should_preserve_the_constructor_failure() => Problems.Single().Cause!.InnerException.ShouldBeOfExactType<InvalidOperationException>();
}
