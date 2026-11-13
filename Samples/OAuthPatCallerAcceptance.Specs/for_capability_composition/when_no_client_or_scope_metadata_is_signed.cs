// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OAuthPatCallerAcceptance.Specs;

public class when_no_client_or_scope_metadata_is_signed : given.a_composed_host
{
    void Establish() => _envelope = _envelope with { Headers = new Dictionary<string, string?> { ["x-cratis-token-scope"] = null, ["x-cratis-token-client-id"] = null } };
    Task Because() => SendBoth();

    [Fact] void should_use_only_independently_verified_metadata_on_mcp_and_rest() => _outcomes.Count(outcome => outcome.Accepted).ShouldEqual(2);
    [Fact] void should_independently_verify_each_request() => _store.OAuthReads.ShouldEqual(2);
    [Fact] void should_dispatch_each_operation_once() => _store.Dispatches.ShouldEqual(2);
}
