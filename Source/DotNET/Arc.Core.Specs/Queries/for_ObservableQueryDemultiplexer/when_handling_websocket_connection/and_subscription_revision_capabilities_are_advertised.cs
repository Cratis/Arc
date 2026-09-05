// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_subscription_revision_capabilities_are_advertised : given.a_guarded_websocket_connection
{
    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_send_connected_before_any_other_server_message() => _sentMessages.First().Type.ShouldEqual(ObservableQueryHubMessageType.Connected);
    [Fact] void should_advertise_subscription_revision_support() =>
        ((bool)typeof(ObservableQueryHubMessage).GetProperty("SupportsSubscriptionRevisions")!.GetValue(_sentMessages.First())!).ShouldBeTrue();
    [Fact] void should_include_the_websocket_connection_id() =>
        ((JsonElement)_sentMessages.First().Payload!).GetString()!.StartsWith("ws-", StringComparison.Ordinal).ShouldBeTrue();
}
