// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WellKnownRoutesMiddleware.when_invoking;

public class with_a_websocket_upgrade_and_an_empty_configured_list : given.a_websocket_upgrade_request
{
    void Establish() => _arcOptions.Query.AllowedWebSocketOrigins = [];

    Task Because() => Upgrade("https://elsewhere.test");

    [Fact] void should_handle_the_request() => _result.ShouldBeTrue();
    [Fact] void should_not_call_next() => _nextCalled.ShouldBeFalse();
    [Fact] void should_not_call_the_handler() => _handlerCalled.ShouldEqual(false);
    [Fact] void should_not_accept_the_connection() => _accepted.ShouldEqual(false);
    [Fact] void should_false() => (_connectError is null).ShouldEqual(false);
}
