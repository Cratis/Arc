// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_connect;

public class and_no_tenant_membership_is_registered : given.a_tenant_admission
{
    void Establish() => _registerMembership = false;
    async Task Because()
    {
        BuildRequestServices();
        await _hub.HandleSSEConnection(_context);
    }
    [Fact] void should_not_add_a_resolver_read() => _resolver.DidNotReceive().Resolve();
    [Fact] void should_preserve_sse_admission() => _context.Received(1).SetResponseHeader("Cache-Control", "no-cache");
    [Fact] void should_not_deny_admission() => _context.DidNotReceive().SetStatusCode(403);
}
