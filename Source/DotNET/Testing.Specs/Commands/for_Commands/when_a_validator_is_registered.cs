// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_validator_is_registered : given.a_wiring_check
{
    void Establish()
    {
        Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(Validator)]);
        Services.AddScoped<IDependency, Dependency>().AddSingleton(new Validator(new Dependency()));
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ValidCommand)]);
    [Fact] void should_accept_the_registered_instance() => Problems.ShouldBeEmpty();
}
