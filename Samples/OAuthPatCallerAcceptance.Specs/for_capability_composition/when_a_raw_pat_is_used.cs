// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OAuthPatCallerAcceptance.Specs;

public class when_a_raw_pat_is_used : given.a_composed_host
{
    void Establish()
    {
        _usePat = true;
        _raw = true;
    }

    Task Because() => SendBoth();

    [Fact] void should_preserve_raw_pat_admission_on_mcp_and_rest() => _outcomes.Count(outcome => outcome.Accepted).ShouldEqual(2);
    [Fact] void should_dispatch_each_operation_once() => _store.Dispatches.ShouldEqual(2);
    [Fact] void should_consult_the_pat_authority_for_each_request() => _store.PatReads.ShouldEqual(2);
    [Fact] void should_not_fall_back_to_oauth() => _store.OAuthReads.ShouldEqual(0);
}
