// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace OAuthPatCallerAcceptance.Specs;

public class when_the_forwarding_peer_has_no_certificate : given.a_refused_call
{
    Task Because() => SendBoth();

    [Fact] void should_attempt_both_transports() => AssertBothAttempted();
    [Fact] void should_refuse_both_transports() => AssertBothRefused();
    [Fact] void should_not_dispatch() => AssertNoDispatch();
    [Fact] void should_use_only_selected_oauth() => AssertOAuthReads();
    [Fact] void should_not_fall_back_to_pat() => AssertPatReads();

    protected override HttpStatusCode? ExpectedStatus => null;
    void Establish() => _noCertificate = true;
}
