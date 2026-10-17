// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_an_anonymous_query_keeps_the_captured_principal : given.a_guarded_websocket_connection
{
    void Establish()
    {
        var replacement = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "replacement")], "test"));
        _context.User.Returns(_principal, replacement);
        _performer.AllowsAnonymousAccess.Returns(true);
        _streamingData = new[] { "snapshot" };
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_keep_the_captured_principal_for_the_guard() => _guardCalls.Single().Principal!.Identity!.Name.ShouldEqual("the-caller");
    [Fact] void should_deliver_the_snapshot() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
}
