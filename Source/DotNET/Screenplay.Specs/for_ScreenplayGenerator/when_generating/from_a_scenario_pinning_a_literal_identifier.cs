// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_scenario_pinning_a_literal_identifier : a_generated_document
{
    const string Id = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";

    void Because() => GeneratePinned("_scenario.PinGeneratedValue(\"author\", _id)");

    [Fact] void should_keep_generation_with_a_pinned_scenario() => Result.Source.ShouldContain("author AuthorId generated identifier");
    [Fact] void should_state_the_scenario() => Result.Source.ShouldContain("specification WhenRegisteringAndAnAuthorIsRegistered");
    [Fact] void should_emit_the_canonical_identifier_fixture() => Result.Source.ShouldContain($"for \"{Id}\"");
    [Fact] void should_keep_given_and_then_on_the_pinned_source_implicit() => Result.Source.Split($"for \"{Id}\"", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_pass_reference_execution() => Run("WhenRegisteringAndAnAuthorIsRegistered").Passed.ShouldBeTrue();

    [Theory]
    [InlineData("_scenario.PinGeneratedValue(\"author\", new AuthorId(Guid.Parse(\"AAAAAAAA-BBBB-4CCC-8DDD-EEEEEEEEEEEE\")))")]
    [InlineData("_scenario.PinGeneratedValue<RegisterAuthor, RegisterAuthor>(\"author\", _id)")]
    public void should_recover_direct_and_issued_command_generic_pins(string pin)
    {
        GeneratePinned(pin);
        Result.Source.ShouldContain($"for \"{Id}\"");
        Result.Source.ShouldContain("specification WhenRegisteringAndAnAuthorIsRegistered");
        AssertDocument();
        Run("WhenRegisteringAndAnAuthorIsRegistered").Passed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("_scenario.PinGeneratedValue(\"author\", AuthorId.New())", "literal")]
    [InlineData("_scenario.AllowGeneration()", "AllowGeneration")]
    [InlineData("_scenario.PinGeneratedValue<RegisterAuthor, OtherCommand>(\"author\", _id)", "nested-command")]
    [InlineData("_scenario.PinGeneratedValue(\"wrong\", _id)", "literal")]
    [InlineData("_scenario.PinGeneratedValue(\"author\", \"aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee\")", "literal")]
    [InlineData("_scenario.PinGeneratedValue(\"author\", Guid.Parse(\"aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee\"))", "literal")]
    [InlineData("_scenario.PinGeneratedValue(\"author\", _id)", "literal", "void Reset() => _id = AuthorId.New();")]
    [InlineData("_scenario.PinGeneratedValue(\"author\", _id); _scenario.PinGeneratedValue(\"author\", _id)", "single unconditional")]
    [InlineData("if (System.DateTime.UtcNow.Year > 2000) _scenario.PinGeneratedValue(\"author\", _id)", "unconditional")]
    [InlineData("", "PinGeneratedValue")]
    public void should_drop_only_the_scenario_with_an_unrecoverable_pin(string pin, string reason, string members = "")
    {
        GeneratePinned(pin, members);
        Result.Source.ShouldContain("author AuthorId generated identifier");
        Result.Source.ShouldNotContain("specification WhenRegisteringAndAnAuthorIsRegistered");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        diagnostic.Message.ShouldContain(reason);
        diagnostic.Location!.ShouldContain("and_an_author_is_registered");
        AssertDocument();
    }

    [Fact]
    void should_explain_the_strategy_refusal_that_prevents_a_literal_pin()
    {
        GeneratePinned("_scenario.PinGeneratedValue(\"author\", _id)", factory: "public static AuthorId New() => new(Guid.Parse(System.Security.Cryptography.RandomNumberGenerator.GetHexString(64, lowercase: true)));");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("specification WhenRegisteringAndAnAuthorIsRegistered");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).Message.ShouldContain("not a recognized");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification)
            .Message.ShouldContain("the generation strategy of 'author' is not recoverable (SP0073)");
        Result.Source.ShouldNotContain("produces AuthorRegistered");
        Result.Source.ShouldContain("handler");
        AssertPartialDocument();
    }

    void GeneratePinned(string pin, string members = "", string factory = "public static AuthorId New() => new(Guid.NewGuid());")
    {
        var command = GeneratedValueSources.With("""
            public record OtherCommand;
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle([GeneratedValue("author")] AuthorId authorId) => (authorId, new(Name));
            }
            """).Replace("public static AuthorId New() => new(Guid.NewGuid());", factory, StringComparison.Ordinal);
        var scenario = $$"""
            using System;
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class and_an_author_is_registered
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                AuthorId _id = new(Guid.Parse("AAAAAAAA-BBBB-4CCC-8DDD-EEEEEEEEEEEE"));
                {{members}}
                void Establish() { {{pin}}; _scenario.Given.ForEventSource(_id).Events(new AuthorRegistered("Earlier")); }
                async Task Because() => await _scenario.Execute(new RegisterAuthor("Apollo"));
                [Fact] Task should_record_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(_id, @event => @event.Name == "Apollo");
            }
            """;
        Generate(
            (Analyzed.SlicePath, command),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
            ("Library/Authors/Registration/when_registering/and_an_author_is_registered.cs", scenario));
    }
}
