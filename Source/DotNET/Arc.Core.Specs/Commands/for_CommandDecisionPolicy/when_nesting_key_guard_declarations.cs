// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

public class when_nesting_key_guard_declarations : Specification
{
    KeyGuardMode _outer;
    KeyGuardMode _inner;
    KeyGuardMode _restored;
    KeyGuardMode _outside;

    void Because()
    {
        using (CommandDecisionPolicy.Begin(typeof(Outer)))
        {
            _outer = CommandDecisionPolicy.KeyGuard;
            using (CommandDecisionPolicy.Begin(typeof(Inner)))
            {
                _inner = CommandDecisionPolicy.KeyGuard;
            }

            _restored = CommandDecisionPolicy.KeyGuard;
        }

        _outside = CommandDecisionPolicy.KeyGuard;
    }

    [Fact] void should_resolve_the_outer_declaration() => _outer.ShouldEqual(KeyGuardMode.On);
    [Fact] void should_resolve_the_inner_declaration_independently() => _inner.ShouldEqual(KeyGuardMode.Observe);
    [Fact] void should_restore_the_outer_declaration() => _restored.ShouldEqual(KeyGuardMode.On);
    [Fact] void should_default_to_off_outside_a_command() => _outside.ShouldEqual(KeyGuardMode.Off);

    [KeyGuard(KeyGuardMode.On)]
    sealed class Outer;

    [KeyGuard(KeyGuardMode.Observe)]
    sealed class Inner;
}
