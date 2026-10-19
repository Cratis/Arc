// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_checking_composite_parts_with_strict_coverage;

public class and_the_key_type_is_registered : given.a_keyed_protected_read
{
    static and_the_key_type_is_registered() => ProtectedReadModelKeyAccessors.Register<RegisteredKey>(
        [new(nameof(RegisteredKey.Account), key => key.Account), new(nameof(RegisteredKey.Region), key => key.Region)]);

    Exception _failure;

    void Establish()
    {
        _keyOptions.Strict = true;
        SelectKey(new RegisteredKey(new("owner"), "*"));
    }

    void Because() => _failure = Catch.Exception(() => Read(false));

    [Fact] void should_reject_the_invalid_part_as_missing() => AssertMissing(_failure, string.Empty);
    [Fact] void should_not_consult_the_reader() => _inner.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_the_sink() => _stored.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_enroll_a_read() => _unit.HasEnrolledDecisionReads.ShouldBeFalse();

    public record RegisteredKey(AccountId Account, string Region);
}
