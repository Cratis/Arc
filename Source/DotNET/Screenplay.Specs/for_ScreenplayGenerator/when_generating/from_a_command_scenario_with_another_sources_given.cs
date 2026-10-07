// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_scenario_with_another_sources_given : a_generated_document
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;

        namespace Library.Authors.Registration;

        [EventType] public record AuthorRegistered(string Name);
        [Command] public record RegisterAuthor([Key] string Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_another_author_has_claimed_the_name
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;
            readonly EventSourceId _otherId = EventSourceId.New();

            void Establish() => _scenario.Given.ForEventSource("other").Events(new AuthorRegistered("Claimed"));
            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("current", "Claimed"));
            [Fact] void should_reject_the_duplicate() => _result.ShouldHaveConstraintViolationFor("unique-author-name");
        }
        """;

    void Because() => GenerateScenario(Slice, Scenario);

    void GenerateScenario(string slice, string scenario) => Generate(
        (Analyzed.SlicePath, slice),
        ("Library/Feature/Slice/when_registering/and_another_author_has_claimed_the_name.cs", scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_preserve_the_other_source() => string.Join('\n', Result.Source.Split('\n').Select(line => line.Trim())).ShouldContain("given AuthorRegistered\nfor \"other\"");
    [Fact] void should_preserve_the_command_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_bind_and_round_trip() => AssertDocument();

    [Fact] void should_omit_an_explicit_symbolic_source()
    {
        GenerateScenario(Slice, Scenario.Replace("ForEventSource(\"other\")", "ForEventSource(_otherId)", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_omit_an_untyped_given_beside_a_typed_destination()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "[Key] int Id", StringComparison.Ordinal), Scenario.Replace("RegisterAuthor(\"current\",", "RegisterAuthor(1,", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_omit_an_explicit_source_without_a_producer_identifier()
    {
        GenerateScenario(Slice.Replace("[Key] string Id, ", string.Empty, StringComparison.Ordinal), Scenario.Replace("RegisterAuthor(\"current\", ", "RegisterAuthor(", StringComparison.Ordinal));
        AssertOmitted();
    }

    void AssertOmitted()
    {
        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertDocument();
    }
}
