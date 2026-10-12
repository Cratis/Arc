// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_identifier_with_a_rejection_given : a_generated_document
{
    void Because()
    {
        var command = GeneratedValueSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle([GeneratedValue] AuthorId authorId)
                {
                    return (authorId, new(Name));
                }
            }
            """);
        const string Scenario = """
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Testing.EventSequences;
            using Xunit;

            namespace Library.Authors.Registration.when_registering;

            public class and_the_name_is_claimed
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                readonly EventSourceId _existing = EventSourceId.New();
                Result _result = null!;

                void Establish() => _scenario.Given.ForEventSource(_existing).Events(new AuthorRegistered("Claimed"));
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("Claimed"));
                [Fact] void should_reject_the_duplicate() => _result.ShouldHaveConstraintViolationFor("unique-author-name");
            }
            """;
        Generate(
            (Analyzed.SlicePath, command),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            ("Library/Authors/Registration/when_registering/and_the_name_is_claimed.cs", Scenario));
    }

    [Fact] void should_omit_the_scenario_whose_given_is_not_on_the_commands_allocated_source() => Result.Source.ShouldNotContain("specification WhenRegisteringAndTheNameIsClaimed");
    [Fact] void should_keep_the_inline_production() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_keep_generation() => Result.Source.ShouldContain("generated identifier");
    [Fact] void should_keep_the_response() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_explain_why_the_sources_cannot_be_stated_faithfully() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning && diagnostic.Message.Contains("event sources cannot be stated faithfully", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_withhold_the_explicit_generation_contract() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeEmpty();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
