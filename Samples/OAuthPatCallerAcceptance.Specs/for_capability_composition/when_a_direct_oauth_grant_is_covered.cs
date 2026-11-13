// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OAuthPatCallerAcceptance.Specs;

public class when_a_direct_oauth_grant_is_covered : given.a_composed_host
{
    Task Because() => SendBoth();

    [Fact] void should_admit_the_same_mapped_person_on_mcp_and_rest() => _outcomes.Count(outcome => outcome.Accepted).ShouldEqual(2);
    [Fact] void should_dispatch_each_operation_once() => _store.Dispatches.ShouldEqual(2);
    [Fact] void should_independently_verify_each_token() => _store.OAuthReads.ShouldEqual(2);
    [Fact] void should_not_consult_the_pat_authority() => _store.PatReads.ShouldEqual(0);
}
