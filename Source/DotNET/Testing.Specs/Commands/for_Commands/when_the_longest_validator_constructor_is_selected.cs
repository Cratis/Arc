// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_the_longest_validator_constructor_is_selected : given.a_wiring_check
{
    void Establish()
    {
        Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(LongestValidator)]);
        Services.AddScoped<IDependency, Dependency>();
    }
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(LongestCommand)]);
    [Fact] void should_not_fall_back_to_the_parameterless_constructor() => Problems.Single().Category.ShouldEqual("activation");
}
