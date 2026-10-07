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

    void GenerateScenario(string slice, string scenario)
    {
        (string Path, string Text)[] sources =
        [
            (Analyzed.SlicePath, slice),
            ("Library/Feature/Slice/when_registering/and_another_author_has_claimed_the_name.cs", scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        ];
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
    }

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

    [Fact] void should_keep_an_undecidable_source_without_an_emitted_identifier()
    {
        GenerateScenario(Slice.Replace("[Key] string Id, ", string.Empty, StringComparison.Ordinal), Scenario
            .Replace("RegisterAuthor(\"current\", ", "RegisterAuthor(", StringComparison.Ordinal)
            .Replace("ForEventSource(\"other\")", "ForEventSource(EventSourceId.New())", StringComparison.Ordinal));
        AssertImplicitSource();
    }

    [Fact] void should_keep_equal_literals_as_the_implicit_own_source()
    {
        GenerateScenario(Slice, Scenario.Replace("RegisterAuthor(\"current\",", "RegisterAuthor(\"other\",", StringComparison.Ordinal));
        AssertImplicitSource();
    }

    [Fact] void should_keep_the_same_stable_symbol_for_given_and_then()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario());
        AssertImplicitSource(runtimeIdentity: true);
        Result.Model.Slices.SelectMany(slice => slice.Specifications).Single().Then.Single().For.ShouldBeNull();
    }

    [Fact] void should_keep_the_same_stable_receiver_for_given_and_then()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario()
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly SourceHolder _holder = new(); class SourceHolder { public readonly EventSourceId Id = EventSourceId.New(); }", StringComparison.Ordinal)
            .Replace("_otherId", "_holder.Id", StringComparison.Ordinal));
        AssertImplicitSource(runtimeIdentity: true);
    }

    [Fact] void should_not_treat_distinct_stable_symbols_as_the_same_source()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario()
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly EventSourceId _otherId = EventSourceId.New(); readonly EventSourceId _currentId = EventSourceId.New();", StringComparison.Ordinal)
            .Replace("RegisterAuthor(_otherId,", "RegisterAuthor(_currentId,", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_not_treat_distinct_stable_receivers_as_the_same_source()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario()
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly SourceHolder _given = new(); readonly SourceHolder _issued = new(); class SourceHolder { public readonly EventSourceId Id = EventSourceId.New(); }", StringComparison.Ordinal)
            .Replace("_otherId", "_given.Id", StringComparison.Ordinal)
            .Replace("RegisterAuthor(_given.Id,", "RegisterAuthor(_issued.Id,", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_state_a_distinct_then_source()
    {
        GenerateScenario(Slice, Scenario
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(\"other\",", StringComparison.Ordinal)
            .Replace("_result.ShouldHaveConstraintViolationFor(\"unique-author-name\")", "_scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"current\", @event => @event.Name == \"Claimed\")", StringComparison.Ordinal));
        string.Join('\n', Result.Source.Split('\n').Select(line => line.Trim())).ShouldContain("then AuthorRegistered\nfor \"current\"");
        AssertDocument();
    }

    static string SameSourceScenario() => Scenario
        .Replace("ForEventSource(\"other\")", "ForEventSource(_otherId)", StringComparison.Ordinal)
        .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(_otherId,", StringComparison.Ordinal)
        .Replace("_result.ShouldHaveConstraintViolationFor(\"unique-author-name\")", "_scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(_otherId, @event => @event.Name == \"Claimed\")", StringComparison.Ordinal);

    void AssertImplicitSource(bool runtimeIdentity = false)
    {
        Result.Source.ShouldContain("specification");
        Result.Model.Slices.SelectMany(slice => slice.Specifications).Single().Given.Single().For.ShouldBeNull();
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        if (runtimeIdentity)
        {
            // Main preserves these scenarios without a concrete when identity; binding needs an identity fixture.
            RoundTrip.Errors.ShouldBeEmpty();
            RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
            RoundTrip.IsStable.ShouldBeTrue();
            return;
        }

        AssertDocument();
    }

    void AssertOmitted()
    {
        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertDocument();
    }
}
