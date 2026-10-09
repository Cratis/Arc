// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public partial class from_a_command_scenario_with_another_sources_given : a_generated_document
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

    [Fact] void should_omit_an_undecidable_source_without_an_emitted_identifier()
    {
        GenerateScenario(Slice.Replace("[Key] string Id, ", string.Empty, StringComparison.Ordinal), Scenario
            .Replace("RegisterAuthor(\"current\", ", "RegisterAuthor(", StringComparison.Ordinal)
            .Replace("ForEventSource(\"other\")", "ForEventSource(EventSourceId.New())", StringComparison.Ordinal));
        AssertOmitted();
    }

    [Fact] void should_keep_equal_literals_as_the_implicit_own_source()
    {
        GenerateScenario(Slice, Scenario.Replace("RegisterAuthor(\"current\",", "RegisterAuthor(\"other\",", StringComparison.Ordinal));
        AssertImplicitSource();
    }

    [Fact] void should_omit_a_generated_command_identity_even_when_given_and_then_share_its_symbol()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario());
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_omit_a_generated_command_identity_even_when_given_and_then_share_its_receiver()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario()
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly SourceHolder _holder = new(); class SourceHolder { public readonly EventSourceId Id = EventSourceId.New(); }", StringComparison.Ordinal)
            .Replace("_otherId", "_holder.Id", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_not_treat_distinct_stable_symbols_as_the_same_source()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario()
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly EventSourceId _otherId = EventSourceId.New(); readonly EventSourceId _currentId = EventSourceId.New();", StringComparison.Ordinal)
            .Replace("RegisterAuthor(_otherId,", "RegisterAuthor(_currentId,", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_not_treat_distinct_stable_receivers_as_the_same_source()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), SameSourceScenario()
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "readonly SourceHolder _given = new(); readonly SourceHolder _issued = new(); class SourceHolder { public readonly EventSourceId Id = EventSourceId.New(); }", StringComparison.Ordinal)
            .Replace("_otherId", "_given.Id", StringComparison.Ordinal)
            .Replace("RegisterAuthor(_given.Id,", "RegisterAuthor(_issued.Id,", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_state_a_distinct_then_source()
    {
        GenerateScenario(Slice, Scenario
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(\"other\",", StringComparison.Ordinal)
            .Replace("_result.ShouldHaveConstraintViolationFor(\"unique-author-name\")", "_scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"current\", @event => @event.Name == \"Claimed\")", StringComparison.Ordinal));
        string.Join('\n', Result.Source.Split('\n').Select(line => line.Trim())).ShouldContain("then AuthorRegistered\nfor \"current\"");
        AssertDocument();
    }

    [Theory]
    [InlineData("new AuthorId(System.Guid.Parse(\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\"))")]
    [InlineData("new AuthorId(new System.Guid(\"6f3c8b4719384d4c8f26817e306a10e2\"))")]
    public void should_omit_a_scenario_whose_concept_identity_is_not_read_as_a_literal(string identity)
    {
        var slice = Slice.Replace("[Key] string Id", "[Key] AuthorId Id", StringComparison.Ordinal) + """

            public record AuthorId(System.Guid Value) : Cratis.Concepts.ConceptAs<System.Guid>(Value)
            {
                public static implicit operator AuthorId(string value) => new(System.Guid.Parse(value));
            }
            """;
        GenerateScenario(slice, Scenario
            .Replace("ForEventSource(\"other\")", "ForEventSource(\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\")", StringComparison.Ordinal)
            .Replace("RegisterAuthor(\"current\",", $"RegisterAuthor({identity},", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Theory]
    [InlineData("\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\"")]
    public void should_keep_a_literal_given_matching_a_constructed_concept(string identity)
    {
        var slice = Slice.Replace("[Key] string Id", "[Key] AuthorId Id", StringComparison.Ordinal) + """

            public record AuthorId(System.Guid Value) : Cratis.Concepts.ConceptAs<System.Guid>(Value)
            {
                public static implicit operator AuthorId(string value) => new(System.Guid.Parse(value));
            }
            """;
        GenerateScenario(slice, Scenario
            .Replace("ForEventSource(\"other\")", "ForEventSource(\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\")", StringComparison.Ordinal)
            .Replace("RegisterAuthor(\"current\",", $"RegisterAuthor({identity},", StringComparison.Ordinal));
        AssertImplicitSource(runtimeIdentity: true);
    }

    [Theory]
    [InlineData("{6F3C8B47-1938-4D4C-8F26-817E306A10E2}")]
    [InlineData("6F3C8B47-1938-4D4C-8F26-817E306A10E2")]
    public void should_state_a_guid_shaped_string_source_without_normalizing_it(string source)
    {
        GenerateScenario(Slice, Scenario
            .Replace("ForEventSource(\"other\")", $"ForEventSource(\"{source}\")", StringComparison.Ordinal)
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\",", StringComparison.Ordinal));
        Result.Source.ShouldContain($"for \"{source}\"");
        AssertDocument();
    }

    [Theory]
    [InlineData("{6F3C8B47-1938-4D4C-8F26-817E306A10E2}")]
    [InlineData("6F3C8B47-1938-4D4C-8F26-817E306A10E2")]
    public void should_omit_a_noncanonical_string_source_beside_a_guid_destination(string source)
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "[Key] System.Guid Id", StringComparison.Ordinal), Scenario
            .Replace("ForEventSource(\"other\")", $"ForEventSource(\"{source}\")", StringComparison.Ordinal)
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(System.Guid.Parse(\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\"),", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Theory]
    [InlineData("System.Guid.Parse(\"{6F3C8B47-1938-4D4C-8F26-817E306A10E2}\")")]
    [InlineData("new System.Guid(\"6F3C8B47-1938-4D4C-8F26-817E306A10E2\")")]
    public void should_omit_a_runtime_guid_source_whose_command_identity_is_not_stated(string source)
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "[Key] System.Guid Id", StringComparison.Ordinal), Scenario
            .Replace("ForEventSource(\"other\")", $"ForEventSource({source})", StringComparison.Ordinal)
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(System.Guid.Parse(\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\"),", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Theory]
    [InlineData("\"{6F3C8B47-1938-4D4C-8F26-817E306A10E2}\"")]
    [InlineData("\"6F3C8B47-1938-4D4C-8F26-817E306A10E2\"")]
    [InlineData("System.Guid.NewGuid().ToString()")]
    public void should_not_equate_held_text_with_the_guid_it_is_converted_to(string initializer)
    {
        var slice = Slice.Replace("[Key] string Id", "[Key] AuthorId Id", StringComparison.Ordinal) + """

            public record AuthorId(System.Guid Value) : Cratis.Concepts.ConceptAs<System.Guid>(Value)
            {
                public static implicit operator AuthorId(string value) => new(System.Guid.Parse(value));
            }
            """;
        GenerateScenario(slice, Scenario
            .Replace("readonly EventSourceId _otherId = EventSourceId.New();", $"readonly string _otherId = {initializer};", StringComparison.Ordinal)
            .Replace("ForEventSource(\"other\")", "ForEventSource(_otherId)", StringComparison.Ordinal)
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(_otherId,", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_omit_a_constructed_event_source_whose_command_identity_is_not_stated()
    {
        GenerateScenario(Slice.Replace("[Key] string Id", "EventSourceId Id", StringComparison.Ordinal), Scenario
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(new EventSourceId(\"other\"),", StringComparison.Ordinal));
        AssertOmitted("RegisterAuthor.Id");
    }

    [Fact] void should_omit_an_undecidable_computed_source_for_a_validation_rejection()
    {
        GenerateValidationScenario();
        AssertOmitted();
    }

    [Fact] void should_omit_an_undecidable_computed_source_for_a_constraint_rejection()
    {
        GenerateScenario(Slice, ComputedSourceScenario());
        AssertOmitted();
    }

    [Fact] void should_omit_an_undecidable_source_with_both_validation_and_constraint_rejections()
    {
        GenerateValidationScenario("[Fact] void should_reject_the_constraint() => _result.ShouldHaveConstraintViolationFor(\"unique-author-name\");");
        AssertOmitted();
    }

    [Fact] void should_omit_an_undecidable_computed_source_for_a_successful_command()
    {
        GenerateScenario(Slice, ComputedSourceScenario()
            .Replace("_result.ShouldHaveConstraintViolationFor(\"unique-author-name\")", "_scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"current\", @event => @event.Name == \"Claimed\")", StringComparison.Ordinal));
        AssertOmitted();
    }

    void GenerateValidationScenario(string additionalAssertion = "")
    {
        const string slice = "using FluentValidation;\n" + Slice + """

            public class RegisterAuthorValidator : Cratis.Arc.Commands.CommandValidator<RegisterAuthor>
            {
                public RegisterAuthorValidator()
                {
                    RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required");
                }
            }
            """;
        var scenario = ComputedSourceScenario()
            .Replace("RegisterAuthor(\"current\", \"Claimed\")", "RegisterAuthor(\"current\", \"\")", StringComparison.Ordinal)
            .Replace("_result.ShouldHaveConstraintViolationFor(\"unique-author-name\")", "_result.ShouldHaveValidationErrorBecauseOf(\"Name is required\")", StringComparison.Ordinal)
            .Replace("[Fact] void should_reject_the_duplicate()", additionalAssertion + "[Fact] void should_reject_the_duplicate()", StringComparison.Ordinal) + """

            public static class ValidationAssertions
            {
                public static void ShouldHaveValidationErrorBecauseOf(this Result result, string reason) { }
            }
            """;
        GenerateScenario(slice, scenario);
    }

    static string ComputedSourceScenario() => Scenario
        .Replace("readonly EventSourceId _otherId = EventSourceId.New();", "EventSourceId _otherId => EventSourceId.New();", StringComparison.Ordinal)
        .Replace("ForEventSource(\"other\")", "ForEventSource(_otherId)", StringComparison.Ordinal);

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
            Result.Model.Slices.SelectMany(slice => slice.Specifications).Single().When!.Values.Select(value => value.Property).ShouldContain("Id");
        }

        AssertDocument();
    }

    void AssertOmitted(string reason = "event sources cannot be stated faithfully")
    {
        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification &&
            diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning &&
            diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
