// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_a_protected_command_enables_read_protection : Specification
{
    Exception? _error;
    CommandDecisionMode _mode;
    ReadProtectionMode _readProtection;

    void Because()
    {
        using var invocation = CommandDecisionPolicy.Begin(typeof(Command));
        _mode = CommandDecisionPolicy.Mode;
        _readProtection = CommandDecisionPolicy.ReadProtection;
        _error = Catch.Exception(CommandDecisionPolicy.ThrowIfConflicting);
    }

    [Fact] void should_keep_the_protected_boundary() => _mode.ShouldEqual(CommandDecisionMode.Protected);
    [Fact] void should_resolve_read_protection_independently() => _readProtection.ShouldEqual(ReadProtectionMode.On);
    [Fact] void should_not_report_a_conflict() => _error.ShouldBeNull();

    [ProtectedForSpecs, ReadProtection(ReadProtectionMode.On)]
    sealed class Command;
}
