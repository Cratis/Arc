// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unrecoverable_generation_with_pins_and_rejections : an_unrecoverable_generated_document
{
    void Because()
    {
        var source = WithUnavailable("""
            [Command] public record RegisterAuthor(string Name)
            {
                public AuthorRegistered Handle([GeneratedValue] AuthorId authorId) => new(authorId, Name);
            }
            """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(AuthorId Copy, string Name);", StringComparison.Ordinal);
        const string Common = """
            using System;
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            """;
        const string Pinned = Common + """
            public class with_a_pin
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                readonly AuthorId _id = new(Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"));
                void Establish() => _scenario.PinGeneratedValue("authorId", _id);
                async Task Because() => await _scenario.Execute(new RegisterAuthor("Apollo"));
                [Fact] Task should_record_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(_id, @event => @event.Name == "Apollo");
            }
            """;
        const string Success = Common + """
            public class without_a_pin
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                readonly AuthorId _id = new(Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"));
                async Task Because() => await _scenario.Execute(new RegisterAuthor("Apollo"));
                [Fact] Task should_record_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(_id, @event => @event.Name == "Apollo");
            }
            """;
        var testing = IntegrationTesting.Source.Replace(
            "public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());",
            "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());",
            StringComparison.Ordinal) + """
            namespace Cratis.Arc.Testing.Commands
            {
                public static class CommandResultShouldExtensions
                {
                    public static void ShouldHaveValidationErrors(this Cratis.Arc.Commands.CommandResult result) { }
                }
            }
            """;
        const string Rejection = Common + """
            public class with_a_rejection
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                Cratis.Arc.Commands.CommandResult _result = null!;
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(""));
                [Fact] void should_reject_the_command() => _result.ShouldHaveValidationErrors();
            }
            """;
        GenerateAcrossOptions(source,
            (IntegrationTesting.Path, testing),
            ("Library/Testing/Pins.cs", GeneratedValueSources.Pins),
            ("Library/Authors/Registration/when_registering/with_a_pin.cs", Pinned),
            ("Library/Authors/Registration/when_registering/without_a_pin.cs", Success),
            ("Library/Authors/Registration/when_registering/with_a_rejection.cs", Rejection));
    }

    [Fact] void should_withhold_the_pinned_scenario() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("specification WhenRegisteringWithAPin", StringComparison.Ordinal));
    [Fact] void should_withhold_the_unpinned_success_after_production_loss() => Documents.ShouldEachConformTo(document => !document.Result.Source.Contains("specification WhenRegisteringWithoutAPin", StringComparison.Ordinal));
    [Fact] void should_keep_the_independent_pre_generation_rejection() => Documents.ShouldEachConformTo(document => document.Result.Source.Contains("specification WhenRegisteringWithARejection", StringComparison.Ordinal) && document.Result.Source.Contains("then error", StringComparison.Ordinal));
    [Fact] void should_report_both_unfaithful_successful_scenarios() => Documents.ShouldEachConformTo(document => document.Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification) == 2);
    [Fact] void should_bind_and_round_trip_without_unexpected_errors() => AssertFaithfulDocuments();
}
