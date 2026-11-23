// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_only_a_non_discoverable_validator_is_registered : given.a_wiring_check
{
    void Establish() => Services.AddSingleton<IValidator<NullableCommand>>(_ => throw new InvalidOperationException("Must not activate arbitrary validators"));
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(NullableCommand)]);
    [Fact] void should_check_only_discoverable_validators() => Problems.ShouldBeEmpty();
}
