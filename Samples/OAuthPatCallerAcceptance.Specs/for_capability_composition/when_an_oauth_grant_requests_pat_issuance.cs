// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace OAuthPatCallerAcceptance.Specs;

public class when_an_oauth_grant_requests_pat_issuance : given.a_composed_host
{
    Task Because() => RequestPatIssuance();

    [Fact] void should_attempt_the_rest_issuance_route() => _outcomes.Single().Path.ShouldEqual("/issue");
    [Fact] void should_refuse_issuance() => (_outcomes.Single().Status == HttpStatusCode.Unauthorized && _outcomes.Single().SafeRefusal).ShouldBeTrue();
    [Fact] void should_independently_verify_the_oauth_grant() => _store.OAuthReads.ShouldEqual(1);
    [Fact] void should_not_attempt_to_persist_a_pat() => _store.PatIssueAttempts.ShouldEqual(0);
    [Fact] void should_not_dispatch_a_capability() => _store.Dispatches.ShouldEqual(0);
}
