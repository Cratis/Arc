// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Flags combined into a value no member is declared with have no name the document could use - it names one member
/// and Screenplay has no form for a combination - and a name made up for them would describe a member the application
/// does not declare. A scenario issuing, starting from or expecting such a value is then not one the document can
/// state exactly, so it is left out and said so, rather than written as the number behind it. A single declared flag
/// is still stated.
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

    const string Usings = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Accounts.Notifications;
        using Xunit;

        namespace Library.Accounts.Notifications.when_updating_preferences;

        """;

    const string ExpectedScenario = Usings + """
        public class and_every_channel_is_expected
        {
            readonly CommandScenario<UpdateNotificationPreferences> _scenario = new();

            async Task Because() => await _scenario.Execute(new UpdateNotificationPreferences(NotificationChannels.Email));

            [Fact] Task should_update_the_preferences() => _scenario.EventSequence.ShouldHaveAppendedEvent<NotificationPreferencesUpdated>(
                "preferences",
                @event => @event.Timesheets == (NotificationChannels.Email | NotificationChannels.InApp));
        }
        """;

    const string IssuedScenario = Usings + """
        public class and_every_channel_is_chosen
        {
            readonly CommandScenario<UpdateNotificationPreferences> _scenario = new();

            async Task Because() => await _scenario.Execute(new UpdateNotificationPreferences(NotificationChannels.Email | NotificationChannels.InApp));

            [Fact] Task should_update_the_preferences() => _scenario.EventSequence.ShouldHaveAppendedEvent<NotificationPreferencesUpdated>(
                "preferences",
                @event => @event.Timesheets == NotificationChannels.Email);
        }
        """;

    const string SeededScenario = Usings + """
        public class and_every_channel_was_chosen_before
        {
            readonly CommandScenario<UpdateNotificationPreferences> _scenario = new();

            void Establish() => _scenario.Given.ForEventSource("preferences").Events(new NotificationPreferencesUpdated(NotificationChannels.Email | NotificationChannels.InApp));

            async Task Because() => await _scenario.Execute(new UpdateNotificationPreferences(NotificationChannels.Email));

            [Fact] Task should_update_the_preferences() => _scenario.EventSequence.ShouldHaveAppendedEvent<NotificationPreferencesUpdated>(
                "preferences",
                @event => @event.Timesheets == NotificationChannels.Email);
        }
        """;

    const string UndeclaredBitScenario = Usings + """
        public class and_a_channel_nobody_declared_is_chosen
        {
            readonly CommandScenario<UpdateNotificationPreferences> _scenario = new();

            async Task Because() => await _scenario.Execute(new UpdateNotificationPreferences(NotificationChannels.Email | (NotificationChannels)8));

            [Fact] Task should_update_the_preferences() => _scenario.EventSequence.ShouldHaveAppendedEvent<NotificationPreferencesUpdated>(
                "preferences",
                @event => @event.Timesheets == NotificationChannels.Email);
        }
        """;

    const string DeclaredScenario = Usings + """
        public class and_one_channel_is_chosen
        {
            readonly CommandScenario<UpdateNotificationPreferences> _scenario = new();

            async Task Because() => await _scenario.Execute(new UpdateNotificationPreferences(NotificationChannels.InApp));

            [Fact] Task should_update_the_preferences() => _scenario.EventSequence.ShouldHaveAppendedEvent<NotificationPreferencesUpdated>(
                "preferences",
                @event => @event.Timesheets == NotificationChannels.InApp);
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Accounts/Notifications/Notifications.cs", Slice),
        ("Library/Accounts/Notifications/when_updating_preferences/and_every_channel_is_expected.cs", ExpectedScenario),
        ("Library/Accounts/Notifications/when_updating_preferences/and_every_channel_is_chosen.cs", IssuedScenario),
        ("Library/Accounts/Notifications/when_updating_preferences/and_every_channel_was_chosen_before.cs", SeededScenario),
        ("Library/Accounts/Notifications/when_updating_preferences/and_a_channel_nobody_declared_is_chosen.cs", UndeclaredBitScenario),
        ("Library/Accounts/Notifications/when_updating_preferences/and_one_channel_is_chosen.cs", DeclaredScenario),
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
    [Fact] void should_state_only_the_scenario_with_a_declared_flag() => Lines().Count(_ => _.StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_never_state_a_combination_as_a_number() => Lines().ShouldNotContain("timesheets = 3");
    [Fact] void should_leave_out_each_scenario_it_cannot_state() => Omissions().Count().ShouldEqual(4);
    [Fact] void should_warn_about_each_of_them() => _result.Diagnostics
        .Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification)
        .All(_ => _.Severity == ScreenplayDiagnosticSeverity.Warning)
        .ShouldBeTrue();
    [Fact] void should_say_a_combination_in_the_expected_event_is_not_a_declared_member() => Reasons().ShouldContain(
        "an expected event predicate states 'NotificationPreferencesUpdated.Timesheets' as 3, a combination of flags of the enumeration 'NotificationChannels' that is not a declared member, and Screenplay cannot state combined flags");
    [Fact] void should_say_a_combination_in_the_command_is_not_a_declared_member() => Reasons().ShouldContain(
        "it states 'UpdateNotificationPreferences.Timesheets' as 3, a combination of flags of the enumeration 'NotificationChannels' that is not a declared member, and Screenplay cannot state combined flags");
    [Fact] void should_say_a_combination_in_the_event_it_starts_from_is_not_a_declared_member() => Reasons().ShouldContain(
        "it states 'NotificationPreferencesUpdated.Timesheets' as 3, a combination of flags of the enumeration 'NotificationChannels' that is not a declared member, and Screenplay cannot state combined flags");
    [Fact] void should_not_call_a_combination_with_an_undeclared_flag_a_combination_of_declared_flags() => Reasons().ShouldContain(
        "it states 'UpdateNotificationPreferences.Timesheets' as 9, which no member of the enumeration 'NotificationChannels' is declared with");

    IEnumerable<string> Omissions() =>
        _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Select(_ => _.Message);

    IEnumerable<string> Reasons() => Omissions().Select(_ => _.Split(" because ")[^1]);
}
