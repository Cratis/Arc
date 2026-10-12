// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_generation_has_no_received_time : given.a_context_generated_value_resolver
{
    Exception _error;

    void Establish() => _context = _context with { ReceivedAt = default };
    async Task Because() => _error = await Catch.Exception(async () => await Resolve());

    [Fact] void should_fail_instead_of_using_the_system_clock() => _error.ShouldBeOfExactType<MissingGenerationTime>();
    [Fact] void should_capture_nothing() => _context.GeneratedValues.ShouldBeEmpty();
}
