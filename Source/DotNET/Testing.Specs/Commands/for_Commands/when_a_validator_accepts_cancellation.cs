// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_validator_accepts_cancellation : given.a_wiring_check
{
    void Establish() => Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(given.handle_bindings.CancellationValidator)]);
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(given.handle_bindings.ValidatorCancellationCommand)]);
    [Fact] void should_not_apply_handle_only_bindings_to_validator_constructors() => Problems.Single().Category.ShouldEqual("missing-service");
}
