// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_a_source_identifier_on_another_stream : a_generated_document
{
    const string Command = """
        public record AccountId(int Value) : Cratis.Concepts.ConceptAs<int>(Value)
        {
            public static implicit operator AccountId(int value) => new(value);
            public static implicit operator EventSourceId(AccountId value) => new(value.Value.ToString());
        }
        [Command, EventSourceType("Account"), EventStreamType("Transactions")]
        public record RegisterAuthor([Key] AccountId Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        [EventType]
        public record AuthorImported(string Name);
        [Command]
        public record ImportAuthor(string Name)
        {
            public AuthorImported Handle() => new(Name);
        }
        """;
    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_an_author_was_imported
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Task Establish() => _scenario.EventScenario.EventSequence.Append(new AccountId(2), new AuthorImported("Prior"),
                eventSourceType: "Account", eventStreamType: "Archive");
            Task Because() => _scenario.Execute(new RegisterAuthor(1, "New"));
            [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(new AccountId(1), e => e.Name == "New");
        }
        """;

    void Because() => Generate(
        (Analyzed.SlicePath, IdentifierSources.With(Command)),
        ("Library/Authors/Registration/when_registering/and_an_author_was_imported.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_state_the_scenario_using_the_sources_declared_identifier() => Result.Source.ShouldContain("specification WhenRegisteringAndAnAuthorWasImported");
    [Fact] void should_state_the_prior_occurrence_on_its_own_stream() => Result.Source.ShouldContain("stream Account.Archive");
    [Fact] void should_keep_the_sources_identifier_type() => Result.Source.ShouldContain("identifier AccountId");
    [Fact] void should_compile_and_bind() => AssertDocument();
}
