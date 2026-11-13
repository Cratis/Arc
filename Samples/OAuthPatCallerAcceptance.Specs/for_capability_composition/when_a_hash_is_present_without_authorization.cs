// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OAuthPatCallerAcceptance.Specs;

public class when_a_hash_is_present_without_authorization : given.a_refused_call
{
    void Establish() => _envelope = _envelope with { OmitBearer = true };
    Task Because() => SendBoth();

    [Fact] void should_attempt_both_transports() => AssertBothAttempted();
    [Fact] void should_refuse_both_transports() => AssertBothRefused();
    [Fact] void should_not_dispatch() => AssertNoDispatch();
    [Fact] void should_not_consult_oauth() => AssertOAuthReads();
    [Fact] void should_not_fall_back_to_pat() => AssertPatReads();
}
