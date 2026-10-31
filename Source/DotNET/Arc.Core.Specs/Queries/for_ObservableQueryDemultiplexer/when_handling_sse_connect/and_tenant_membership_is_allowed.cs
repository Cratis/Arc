// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_connect;

public class and_tenant_membership_is_allowed : given.a_tenant_admission
{
    void Establish() => _allow = true;
    async Task Because()
    {
        BuildRequestServices();
        await _hub.HandleSSEConnection(_context);
    }
    [Fact] void should_admit_sse() => _context.Received(1).SetResponseHeader("Cache-Control", "no-cache");
    [Fact] void should_resolve_the_candidate_once() => _resolver.Received(1).Resolve();
    [Fact] void should_not_deny_admission() => _context.DidNotReceive().SetStatusCode(403);
}
