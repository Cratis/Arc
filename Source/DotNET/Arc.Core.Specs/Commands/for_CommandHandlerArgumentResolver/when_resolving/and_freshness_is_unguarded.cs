// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_freshness_is_unguarded : given.a_generated_value_resolver
{
    void Establish() => For<CreatesUnguardedId>();

    async Task Because() => _resolution = await Resolve();

    [Fact] void should_capture_the_opt_out() => _context.GeneratedValues.Single().Freshness.ShouldEqual(GeneratedValueFreshness.Unguarded);
    [Fact] void should_still_generate_the_value() => _resolution.Arguments.Single().ShouldBeOfExactType<Id>();

    public class CreatesUnguardedId
    {
        public void Handle([GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)] Id id) { }
    }
}
