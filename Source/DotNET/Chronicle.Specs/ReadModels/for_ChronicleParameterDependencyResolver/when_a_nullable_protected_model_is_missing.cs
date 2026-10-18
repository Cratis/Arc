// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_nullable_protected_model_is_missing : given.a_keyed_protected_read
{
    object? _instance;

    void Establish()
    {
        SelectKey(new AccountId("missing"));
        StubRead("missing", null);
    }

    void Because() => _instance = Read(true);

    [Fact] void should_receive_null() => _instance.ShouldBeNull();
    [Fact] void should_keep_the_absence_guard() => _unit.HasEnrolledDecisionReads.ShouldBeTrue();
    [Fact] void should_not_read_the_sink() => _stored.ReceivedCalls().ShouldBeEmpty();
}
