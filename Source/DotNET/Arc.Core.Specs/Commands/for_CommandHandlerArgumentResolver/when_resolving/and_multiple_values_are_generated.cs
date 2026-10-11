// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_multiple_values_are_generated : given.a_generated_value_resolver
{
    void Establish() => For<CreatesTwoIds>();
    async Task Because() => _resolution = await Resolve();

    [Fact] void should_capture_in_parameter_order() => _context.GeneratedValues.Select(value => value.Name).ShouldContainOnly("first", "second");
    [Fact] void should_assign_resolution_order() => _context.GeneratedValues.Select(value => value.Order).ShouldContainOnly(0, 1);
    [Fact] void should_capture_the_declared_types() => _context.GeneratedValues.Select(value => value.Type).ShouldContainOnly(typeof(Id), typeof(Id));
    [Fact] void should_keep_each_resolved_argument() => _context.GeneratedValues.Select(value => value.Value).ShouldContainOnly(_resolution.Arguments);
}
