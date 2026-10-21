// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Provides a routed command and a normal command scenario for stored-name admission specifications.
/// </summary>
internal static class StoredRouteScenarioSources
{
    /// <summary>
    /// The scenario, whose optional prior append is replaced by each specification.
    /// </summary>
    internal const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_it_succeeds
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            GIVEN
            Task Because() => _scenario.Execute(new RegisterAuthor("author", "New"));
            [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == "New");
        }
        """;

    /// <summary>
    /// Builds a command whose model names differ from its runtime stored names.
    /// </summary>
    /// <param name="source">The stored source name.</param>
    /// <param name="stream">The stored stream name.</param>
    /// <returns>The command source.</returns>
    internal static string Command(string source, string stream) => IdentifierSources.With($$"""
        [Cratis.Chronicle.EventSources.EventSource("{{source}}")]
        [Cratis.Chronicle.EventSources.EventStream(AccountEventSource.Transactions)]
        public class AccountEventSource : Cratis.Chronicle.EventSources.IEventSource
        {
            public const string Transactions = "{{stream}}";
        }
        [Command, Cratis.Arc.Chronicle.Commands.EventSource<AccountEventSource>(AccountEventSource.Transactions)]
        public record RegisterAuthor([Key] string Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """);
}
