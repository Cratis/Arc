// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_resolving_the_profile : Specification
{
    CommandDecisionMode _protected;
    CommandDecisionMode _conflicting;
    Exception? _conflict;
    Exception? _noConflict;
    CommandDecisionMode _unmarked;
    CommandDecisionMode _outside;

    void Because()
    {
        using (CommandDecisionPolicy.Begin(typeof(ProtectedCommand)))
        {
            _protected = CommandDecisionPolicy.Mode;
            _noConflict = Record.Exception(CommandDecisionPolicy.ThrowIfConflicting);
        }

        using (CommandDecisionPolicy.Begin(typeof(ConflictingCommand)))
        {
            _conflicting = CommandDecisionPolicy.Mode;
            _conflict = Record.Exception(CommandDecisionPolicy.ThrowIfConflicting);
        }

        using (CommandDecisionPolicy.Begin(typeof(UnmarkedCommand)))
        {
            _unmarked = CommandDecisionPolicy.Mode;
        }

        _outside = CommandDecisionPolicy.Mode;
    }

    [Fact] void should_resolve_a_protected_command() => _protected.ShouldEqual(CommandDecisionMode.Protected);
    [Fact] void should_not_report_a_conflict_for_a_single_profile() => _noConflict.ShouldBeNull();
    [Fact] void should_fail_closed_as_protected_for_conflicting_profiles() => _conflicting.ShouldEqual(CommandDecisionMode.Protected);
    [Fact] void should_report_conflicting_profiles_when_asked() => _conflict.ShouldBeOfExactType<CommandCannotBeBothProtectedAndUnprotected>();
    [Fact] void should_only_honour_marker_attributes_not_the_interface_on_the_command() => _unmarked.ShouldEqual(CommandDecisionMode.Legacy);
    [Fact] void should_restore_legacy_outside_a_command() => _outside.ShouldEqual(CommandDecisionMode.Legacy);
}
