// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// States event scenarios with explicit sources independently of their payloads.
/// </summary>
public static class EventAppendSources
{
    /// <summary>Gets a producer with an unambiguous textual destination type.</summary>
    public static string Producer => IdentifierSources.With("""
        [Command]
        public record RegisterAuthor([Key] string Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """);

    /// <summary>Builds a scenario with the supplied source expressions.</summary>
    /// <param name="given">The prior occurrence's source expression.</param>
    /// <param name="when">The appended occurrence's source expression.</param>
    /// <param name="then">The asserted occurrence's source expression.</param>
    /// <param name="direct">Whether to append directly to the sequence.</param>
    /// <returns>The scenario source.</returns>
    public static string With(string given, string when, string then, bool direct = false) => $$"""
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_appending;

        public class and_it_succeeds
        {
            readonly EventScenario _scenario = new();
            readonly EventSourceId _first = EventSourceId.New();
            readonly EventSourceId _second = EventSourceId.New();

            async Task Establish() => await _scenario.Given.ForEventSource({{given}}).Events(new AuthorRegistered("Prior"));

            async Task Because() => await {{(direct ? $"_scenario.EventSequence.Append({when}, new AuthorRegistered(\"Jane Austen\"))" : $"_scenario.When.ForEventSource({when}).Events(new AuthorRegistered(\"Jane Austen\"))")}};

            [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>({{then}}, e => e.Name == "Jane Austen");
        }
        """;
}
