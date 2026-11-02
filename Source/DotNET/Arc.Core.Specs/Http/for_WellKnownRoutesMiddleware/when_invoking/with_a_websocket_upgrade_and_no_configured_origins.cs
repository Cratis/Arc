// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WellKnownRoutesMiddleware.when_invoking;

public class with_a_websocket_upgrade_and_no_configured_origins : given.a_websocket_upgrade_request
{
    void Establish() => _arcOptions.Query.AllowedWebSocketOrigins.Clear();

    Task Because() => Upgrade("https://elsewhere.test");

    [Fact] void should_handle_the_request() => _result.ShouldBeTrue();
    [Fact] void should_not_call_next() => _nextCalled.ShouldBeFalse();
    [Fact] void should_call_the_handler() => _handlerCalled.ShouldEqual(true);
    [Fact] void should_accept_the_connection() => _accepted.ShouldEqual(true);
    [Fact] void should_complete_the_upgrade() => (_connectError is null).ShouldEqual(true);
}
