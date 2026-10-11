// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_provide_short_circuits_before_generation : given.a_generated_value_resolver
{
    void Establish() => ProvideReturns(ValidationResult.Error("Rejected before generation"));
    async Task Because() => _resolution = await Resolve();

    [Fact] void should_reject_the_command() => _resolution.ControlResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_build_arguments() => _resolution.Arguments.ShouldBeEmpty();
    [Fact] void should_not_capture_generated_values() => _context.GeneratedValues.ShouldBeEmpty();
    [Fact] void should_not_resolve_the_fixture() => _serviceProvider.DidNotReceive().GetService(typeof(IGeneratedValueFixture));
}
