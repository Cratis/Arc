// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OAuthPatCallerAcceptance.Specs;

public class when_a_pat_is_covered : given.a_composed_host
{
    void Establish() => _usePat = true;
    Task Because() => SendBoth();

    [Fact] void should_preserve_covered_pat_admission_on_mcp_and_rest() => _outcomes.Count(outcome => outcome.Accepted).ShouldEqual(2);
    [Fact] void should_consult_the_pat_authority_once_per_request() => _store.PatReads.ShouldEqual(2);
    [Fact] void should_not_fall_back_to_oauth() => _store.OAuthReads.ShouldEqual(0);
    [Fact] void should_dispatch_each_operation_once() => _store.Dispatches.ShouldEqual(2);
}
