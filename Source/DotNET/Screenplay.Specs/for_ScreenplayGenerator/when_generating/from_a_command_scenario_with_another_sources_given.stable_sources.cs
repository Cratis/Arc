// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public partial class from_a_command_scenario_with_another_sources_given
{
    [Theory]
    [InlineData("readonly EventSourceId _otherId = new(\"other\"); readonly EventSourceId _secondId = new(\"second\");")]
    [InlineData("EventSourceId _otherId { get; } = new(\"other\"); EventSourceId _secondId { get; } = new(\"second\");")]
    public void should_state_distinct_stable_constant_initializers(string declarations)
    {
        GenerateScenario(Slice, TwoSourcesScenario(declarations));
        var text = string.Join('\n', Result.Source.Split('\n').Select(line => line.Trim()));
        text.ShouldContain("given AuthorRegistered\nfor \"other\"");
        text.ShouldContain("given AuthorRegistered\nfor \"second\"");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        AssertDocument();
    }

    [Fact] void should_omit_stable_constant_sources_incompatible_with_the_producer_destination()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "[Key] int Id", StringComparison.Ordinal),
            TwoSourcesScenario().Replace("RegisterAuthor(\"current\",", "RegisterAuthor(1,", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_omit_distinct_constant_members_whose_command_identity_is_not_stated()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario()
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly EventSourceId _otherId = new(\"same\"); readonly EventSourceId _currentId = new(\"same\");", StringComparison.Ordinal)
            .Replace("RegisterAuthor(_otherId,", "RegisterAuthor(_currentId,", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_keep_the_commands_own_source_implicit_when_its_identifier_is_not_emitted()
    {
        GenerateScenario(Slice, Scenario.Replace("RegisterAuthor(\"current\",", "RegisterAuthor(\"other\",", StringComparison.Ordinal));
        var model = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with { Identifier = null }).ToList()
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(model, new());
        emitted.Source.ShouldContain("specification");
        emitted.Source.ShouldNotContain("for \"other\"");
        emitted.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
    }

    [Fact] void should_omit_an_undecidable_computed_source_for_an_authorization_rejection()
    {
        var scenario = ComputedSourceScenario().Replace("ShouldHaveConstraintViolationFor(\"unique-author-name\")", "ShouldNotBeAuthorized()", StringComparison.Ordinal) + """

            public static class AuthorizationAssertions
            {
                public static void ShouldNotBeAuthorized(this Result result) { }
            }
            """;
        GenerateScenario(Slice, scenario);
        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification &&
            diagnostic.Message.Contains("ShouldNotBeAuthorized", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    [Fact] void should_omit_two_distinct_sources_without_an_identifier()
    {
        GenerateScenario(Slice.Replace("[Key] string Id, ", string.Empty, StringComparison.Ordinal),
            TwoSourcesScenario().Replace("RegisterAuthor(\"current\", ", "RegisterAuthor(", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_omit_a_single_concrete_source_without_a_command_source()
    {
        GenerateScenario(Slice.Replace("[Key] string Id, ", string.Empty, StringComparison.Ordinal),
            Scenario.Replace("RegisterAuthor(\"current\", ", "RegisterAuthor(", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_resolve_a_constant_within_a_wrapped_initializer()
    {
        GenerateScenario(Slice, Scenario
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly EventSourceId _otherId = new(_source); const string _source = \"other\";", StringComparison.Ordinal)
            .Replace("ForEventSource(\"other\")", "ForEventSource(_otherId)", StringComparison.Ordinal));
        Result.Source.ShouldContain("for \"other\"");
        AssertDocument();
    }

    [Fact] void should_not_follow_another_held_member()
    {
        GenerateScenario(Slice, Scenario
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly EventSourceId _otherId = _source; static readonly EventSourceId _source = new(\"other\");", StringComparison.Ordinal)
            .Replace("ForEventSource(\"other\")", "ForEventSource(_otherId)", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_omit_a_stable_held_command_with_a_generated_identifier()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), HeldCommandScenario());
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_omit_a_reassigned_held_commands_identifier()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), HeldCommandScenario()
            .Replace("readonly RegisterAuthor _command = new(EventSourceId.New(), \"Claimed\");", "RegisterAuthor _command = new(EventSourceId.New(), \"Claimed\");", StringComparison.Ordinal)
            .Replace("void Establish() =>", "void Establish() { _command = new(EventSourceId.New(), \"Claimed\");", StringComparison.Ordinal)
            .Replace("Events(new AuthorRegistered(\"Claimed\"));", "Events(new AuthorRegistered(\"Claimed\")); }", StringComparison.Ordinal));
        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertDocument();
    }

    [Fact] void should_omit_another_held_commands_positional_identifier()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), HeldCommandScenario()
            .Replace("readonly RegisterAuthor _command =", "readonly RegisterAuthor _otherCommand = new(EventSourceId.New(), \"Claimed\"); readonly RegisterAuthor _command =", StringComparison.Ordinal)
            .Replace("ForEventSource(_command.Id)", "ForEventSource(_otherCommand.Id)", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    static string HeldCommandScenario() => Scenario
        .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly RegisterAuthor _command = new(EventSourceId.New(), \"Claimed\");", StringComparison.Ordinal)
        .Replace("ForEventSource(\"other\")", "ForEventSource(_command.Id)", StringComparison.Ordinal)
        .Replace("Execute(new RegisterAuthor(\"current\", \"Claimed\"))", "Execute(_command)", StringComparison.Ordinal);

    static string TwoSourcesScenario(string declarations = "readonly EventSourceId _otherId = new(\"other\"); readonly EventSourceId _secondId = new(\"second\");")
    {
        const string establish = """
            void Establish()
            {
                _scenario.Given.ForEventSource(_otherId).Events(new AuthorRegistered("Claimed"));
                _scenario.Given.ForEventSource(_secondId).Events(new AuthorRegistered("Second"));
            }
            """;

        return Scenario
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", declarations, StringComparison.Ordinal)
            .Replace("void Establish() => _scenario.Given.ForEventSource(\"other\").Events(new AuthorRegistered(\"Claimed\"));", establish, StringComparison.Ordinal);
    }
}
