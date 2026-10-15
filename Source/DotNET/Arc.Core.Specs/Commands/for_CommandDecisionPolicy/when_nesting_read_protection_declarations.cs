// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_nesting_read_protection_declarations : Specification
{
    ReadProtectionMode _outer;
    ReadProtectionMode _inner;
    ReadProtectionMode _restored;
    ReadProtectionMode _outside;

    void Because()
    {
        using (CommandDecisionPolicy.Begin(typeof(Outer)))
        {
            _outer = CommandDecisionPolicy.ReadProtection;
            using (CommandDecisionPolicy.Begin(typeof(Inner)))
            {
                _inner = CommandDecisionPolicy.ReadProtection;
            }

            _restored = CommandDecisionPolicy.ReadProtection;
        }

        _outside = CommandDecisionPolicy.ReadProtection;
    }

    [Fact] void should_resolve_the_outer_declaration() => _outer.ShouldEqual(ReadProtectionMode.On);
    [Fact] void should_resolve_the_inner_declaration_independently() => _inner.ShouldEqual(ReadProtectionMode.Measure);
    [Fact] void should_restore_the_outer_declaration() => _restored.ShouldEqual(ReadProtectionMode.On);
    [Fact] void should_default_to_off_outside_a_command() => _outside.ShouldEqual(ReadProtectionMode.Off);

    [ReadProtection(ReadProtectionMode.On)]
    sealed class Outer;

    [ReadProtection(ReadProtectionMode.Measure)]
    sealed class Inner;
}
