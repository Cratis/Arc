// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// The time the triggering event occurred is the one value of its context a reaction can read, as
/// <c>$context.occurred</c>; anything else the context carries is code.
/// </summary>
public class from_a_reactor_mapping_the_occurrence_time : a_reacting_application
{
    const string Reactor = """
        public class Greeter : IReactor
        {
            public AuthorGreeted Greet(AuthorRegistered @event, EventContext context) => new(@event.Name, context.Occurred);
        }
        """;

    void Because() => GenerateWith(Reactor);

    [Fact] void should_map_the_occurrence_time() => Result.Source.ShouldContain("""
              reaction Greeter
                when AuthorRegistered
                  produces AuthorGreeted
                    name = name
                    greetedAt = $context.occurred
        """);

    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
