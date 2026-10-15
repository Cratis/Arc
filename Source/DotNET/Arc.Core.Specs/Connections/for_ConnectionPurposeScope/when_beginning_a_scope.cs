// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionPurposeScope;

public class when_beginning_a_scope : Specification
{
    ConnectionPurpose _before;
    ConnectionPurpose _inside;
    ConnectionPurpose _after;

    void Because()
    {
        _before = ConnectionPurposeScope.Current;
        using (ConnectionPurposeScope.Begin(ConnectionPurpose.Schema))
        {
            _inside = ConnectionPurposeScope.Current;
        }

        _after = ConnectionPurposeScope.Current;
    }

    [Fact] void should_default_to_runtime() => _before.ShouldEqual(ConnectionPurpose.Runtime);
    [Fact] void should_use_the_purpose_inside_the_scope() => _inside.ShouldEqual(ConnectionPurpose.Schema);
    [Fact] void should_restore_the_previous_purpose() => _after.ShouldEqual(ConnectionPurpose.Runtime);
}
