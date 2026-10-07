// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_identifier_with_a_rejection_given : a_generated_document
{
    void Because()
    {
        var command = IdentifierSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle()
                {
                    AuthorId authorId = new(Guid.NewGuid());
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

    [Fact] void should_keep_the_rejection_scenario() => Result.Source.ShouldContain("specification WhenRegisteringAndTheNameIsClaimed");
    [Fact] void should_keep_the_given() => Result.Source.ShouldContain("given AuthorRegistered");
    [Fact] void should_keep_the_given_name() => Result.Source.ShouldContain("name = \"Claimed\"");
    [Fact] void should_keep_the_rejection() => Result.Source.ShouldContain("then error \"unique-author-name\"");
    [Fact] void should_keep_the_legacy_production() => Result.Source.ShouldContain("produces AuthorRegistered");
    [Fact] void should_withhold_generation() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_withhold_the_identifier() => Result.Source.ShouldNotContain("identifier");
    [Fact] void should_withhold_the_response() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_not_drop_the_scenario() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
    [Fact] void should_explain_why_generation_was_withheld() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("RegisterAuthor", StringComparison.Ordinal) && diagnostic.Message.Contains("explicit given event sources", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
