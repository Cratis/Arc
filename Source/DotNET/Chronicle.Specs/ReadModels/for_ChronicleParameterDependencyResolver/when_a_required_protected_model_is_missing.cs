// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_required_protected_model_is_missing : given.a_keyed_protected_read
{
    Exception _failure;

    void Establish()
    {
        SelectKey(new AccountId("missing"));
        StubRead("missing", null);
    }

    void Because() => _failure = Catch.Exception(() => Read(false));

    [Fact] void should_reject_as_missing_with_a_validation_reason() => AssertMissing(_failure, "missing");
    [Fact] void should_not_read_the_sink() => _stored.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_have_acquired_the_absence_guard() => _unit.HasEnrolledDecisionReads.ShouldBeTrue();
}
