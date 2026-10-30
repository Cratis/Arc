// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_connect;

public class and_tenant_resolution_fails : given.a_tenant_admission
{
    void Establish() => _resolverFails = true;
    async Task Because()
    {
        BuildRequestServices();
        await _hub.HandleSSEConnection(_context);
    }
    [Fact] void should_respond_forbidden() => _context.Received(1).SetStatusCode(403);
    [Fact] void should_not_commit_sse_headers() => _context.DidNotReceive().SetResponseHeader("Cache-Control", "no-cache");
}
