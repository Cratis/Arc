// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentConflicts.when_mapping;

public class without_an_active_command_unit : given.a_recorded_guard
{
    void Because()
    {
        CommandTransaction.Current = null;
        _handled = _mapper.TryMap(_context, _conflict, out _mapped);
    }

    [Fact] void should_not_map_outside_the_command_unit() => _handled.ShouldBeFalse();
    [Fact] void should_leave_default_conversion_available() => _mapped.ShouldBeNull();
}
