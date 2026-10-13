// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_type_has_both_generation_contracts : given.a_context_generated_value_resolver
{
    void Establish()
    {
        For<CreatesDualId>();
        _context = _context with { ReceivedAt = _now };
    }

    async Task Because() => _resolution = await Resolve();

    [Fact] void should_prefer_context_through_the_reflection_fallback() => ((DualId)_resolution.Arguments.Single()!).Value.ShouldEqual($"{_now:O}|tenant-one");
}
