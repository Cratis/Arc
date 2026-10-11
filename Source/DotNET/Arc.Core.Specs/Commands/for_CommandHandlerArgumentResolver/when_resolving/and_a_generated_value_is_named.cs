// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_generated_value_is_named : given.a_generated_value_resolver
{
    void Establish() => For<CreatesNamedId>();
    async Task Because() => _resolution = await Resolve();

    [Fact] void should_capture_by_the_explicit_name() => _context.GeneratedValues.Get<Id>("cart").ShouldEqual(_resolution.Arguments.Single());
    [Fact] void should_use_the_explicit_name_in_metadata() => _context.GeneratedValues.Single().Name.ShouldEqual("cart");
}
