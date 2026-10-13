// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_generation_uses_context : given.a_context_generated_value_resolver
{
    async Task Because() => _resolution = await Resolve();

    [Fact] void should_use_received_time_and_tenant() => ((ContextId)_resolution.Arguments.Single()!).Value.ShouldEqual($"{_now:O}|tenant-one");
    [Fact] void should_capture_the_context_generated_argument() => _context.GeneratedValues.Get<ContextId>("id").ShouldEqual(_resolution.Arguments.Single());
}
