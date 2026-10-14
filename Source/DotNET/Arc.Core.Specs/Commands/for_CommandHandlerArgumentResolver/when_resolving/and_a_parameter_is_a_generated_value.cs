// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_parameter_is_a_generated_value : given.a_generated_value_resolver
{
    async Task Because() => _resolution = await Resolve();

    [Fact] void should_capture_guarded_freshness_by_default() => _context.GeneratedValues.Single().Freshness.ShouldEqual(GeneratedValueFreshness.Guarded);
    [Fact] void should_succeed() => _resolution.ControlResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_generate_the_argument() => _resolution.Arguments.Single().ShouldBeOfExactType<Id>();
    [Fact] void should_capture_the_argument() => _context.GeneratedValues.Get<Id>("id").ShouldEqual(_resolution.Arguments.Single());
    [Fact] void should_keep_captures_in_context_copies() => (_context with { Dependencies = _resolution.Arguments }).GeneratedValues.ShouldEqual(_context.GeneratedValues);
}
