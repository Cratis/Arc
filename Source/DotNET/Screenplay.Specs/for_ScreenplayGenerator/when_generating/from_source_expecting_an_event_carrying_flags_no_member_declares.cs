// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Flags combined into a value no member is declared with have no name the document could use, and a name made up for
/// them would describe a member the application does not declare. The expectation is then not one the document can
/// state exactly, so it is left out and said so, rather than written as the number behind it.
/// </summary>
public class from_source_expecting_an_event_carrying_flags_no_member_declares : Specification
{
    const string Slice = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Accounts.Notifications;

        [Flags]
        public enum NotificationChannels
        {
            None = 0,
            Email = 1 << 0,
            InApp = 1 << 1
        }

        [EventType]
        public record NotificationPreferencesUpdated(NotificationChannels Timesheets);

        [Command]
        public record UpdateNotificationPreferences(NotificationChannels Timesheets)
        {
            public NotificationPreferencesUpdated Handle() => new(Timesheets);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Accounts.Notifications;
        using Xunit;

        namespace Library.Accounts.Notifications.when_updating_preferences;

        public class and_every_channel_is_chosen
        {
            readonly CommandScenario<UpdateNotificationPreferences> _scenario = new();

            async Task Because() => await _scenario.Execute(new UpdateNotificationPreferences(NotificationChannels.Email));

            [Fact] Task should_update_the_preferences() => _scenario.EventSequence.ShouldHaveAppendedEvent<NotificationPreferencesUpdated>(
                "preferences",
                @event => @event.Timesheets == (NotificationChannels.Email | NotificationChannels.InApp));
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Accounts/Notifications/Notifications.cs", Slice),
        ("Library/Accounts/Notifications/when_updating_preferences/and_every_channel_is_chosen.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    IEnumerable<string> Lines() =>
        _result.Source.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim());

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_never_state_the_combination_as_a_number() => Lines().ShouldNotContain("timesheets = 3");
    [Fact] void should_never_state_the_expected_event_with_a_value_it_cannot_name() => Lines().ShouldNotContain("then NotificationPreferencesUpdated");
    [Fact] void should_say_which_value_no_member_is_declared_with() => Reasons().ShouldContain(
        "an expected event predicate states 'NotificationPreferencesUpdated.Timesheets' as 3, which no member of the enumeration 'NotificationChannels' is declared with");

    IEnumerable<string> Reasons() =>
        _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Select(_ => _.Message.Split(" because ")[^1]);
}
