// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A scenario expecting an event states what the event carries in the predicate it is found by, and the compiler hands
/// a member of an enumeration compared there over as the number behind it. The document declares the enumeration by
/// its members, so the expectation has to name the member - the zero member of a set of flags and a member declared as
/// a combination of others included - just as the command the scenario issues does.
/// </summary>
public class from_source_expecting_an_event_carrying_members_of_an_enumeration : Specification
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
            InApp = 1 << 1,
            EmailAndInApp = Email | InApp
        }

        [EventType]
        public record NotificationPreferencesUpdated(NotificationChannels Deadlines, NotificationChannels Timesheets);

        [Command]
        public record UpdateNotificationPreferences(NotificationChannels Deadlines, NotificationChannels Timesheets)
        {
            public NotificationPreferencesUpdated Handle() => new(Deadlines, Timesheets);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Accounts.Notifications;
        using Xunit;

        namespace Library.Accounts.Notifications.when_updating_preferences;

        public class and_only_timesheet_reminders_are_kept
        {
            readonly CommandScenario<UpdateNotificationPreferences> _scenario = new();

            async Task Because() => await _scenario.Execute(
                new UpdateNotificationPreferences(NotificationChannels.None, NotificationChannels.EmailAndInApp));

            [Fact] Task should_update_the_preferences() => _scenario.EventSequence.ShouldHaveAppendedEvent<NotificationPreferencesUpdated>(
                "preferences",
                @event => @event.Deadlines == NotificationChannels.None &&
                          @event.Timesheets == NotificationChannels.EmailAndInApp);
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Accounts/Notifications/Notifications.cs", Slice),
        ("Library/Accounts/Notifications/when_updating_preferences/and_only_timesheet_reminders_are_kept.cs", Scenario),
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
    [Fact] void should_state_the_event_the_scenario_expects() => Lines().ShouldContain("then NotificationPreferencesUpdated");
    [Fact] void should_name_the_zero_member_for_the_command_and_the_event_alike() => Lines().Count(_ => _ == @"deadlines = ""none""").ShouldEqual(2);
    [Fact] void should_name_the_declared_combination_for_the_command_and_the_event_alike() => Lines().Count(_ => _ == @"timesheets = ""emailAndInApp""").ShouldEqual(2);
    [Fact] void should_never_state_the_zero_member_as_a_number() => Lines().ShouldNotContain("deadlines = 0");
    [Fact] void should_never_state_the_declared_combination_as_a_number() => Lines().ShouldNotContain("timesheets = 3");
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
