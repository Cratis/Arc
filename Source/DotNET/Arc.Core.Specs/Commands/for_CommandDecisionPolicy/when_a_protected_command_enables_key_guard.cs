// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_a_protected_command_enables_key_guard : Specification
{
    Exception? _error;
    CommandDecisionMode _mode;
    KeyGuardMode _keyGuard;

    void Because()
    {
        using var invocation = CommandDecisionPolicy.Begin(typeof(Command));
        _mode = CommandDecisionPolicy.Mode;
        _keyGuard = CommandDecisionPolicy.KeyGuard;
        _error = Catch.Exception(CommandDecisionPolicy.ThrowIfConflicting);
    }

    [Fact] void should_keep_the_protected_boundary() => _mode.ShouldEqual(CommandDecisionMode.Protected);
    [Fact] void should_disable_the_key_guard() => _keyGuard.ShouldEqual(KeyGuardMode.Off);
    [Fact] void should_report_the_conflict_through_the_existing_boundary() => _error.ShouldBeOfExactType<CommandCannotCombineProtectedDecisionAndKeyGuard>();

    [ProtectedForSpecs, KeyGuard(KeyGuardMode.On)]
    sealed class Command;
}
