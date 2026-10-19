// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_rejecting_invalid_protected_keys : given.a_keyed_protected_read
{
    public static TheoryData<string, bool, bool> InvalidKeys
    {
        get
        {
            var cases = new TheoryData<string, bool, bool>();
            foreach (var key in new[] { "", "   ", "\t", "*", "a#b", " padded", "padded ", " padded " })
            {
                foreach (var nullable in new[] { false, true })
                {
                    foreach (var stored in new[] { false, true })
                    {
                        cases.Add(key, nullable, stored);
                    }
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidKeys))]
    public void should_reject_before_acquiring_state(string key, bool nullable, bool stored)
    {
        SelectKey(new AccountId(key));
        StubRead(key, stored ? new Balance(new(key)) : null);

        var failure = Catch.Exception(() => Read(nullable));

        AssertMissing(failure, key);
        _inner.ReceivedCalls().ShouldBeEmpty();
        _stored.ReceivedCalls().ShouldBeEmpty();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }
}
