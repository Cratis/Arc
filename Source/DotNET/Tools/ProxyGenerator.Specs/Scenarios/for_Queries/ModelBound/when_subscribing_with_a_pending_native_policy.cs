// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_subscribing_with_a_pending_native_policy : given.a_scenario_web_application
{
    PolicyGate _gate => Host!.Services.GetRequiredService<PolicyGate>();
    ObservableQueryHubMessageType _messageAfterRelease;
    bool _receivedWhilePending;
    int _performedBefore;
    int _performedWhilePending;

    void Establish()
    {
        _gate.Reset();
        _performedBefore = GatedReadModel.Performed;
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("X-Default", "active");
        var uri = new Uri($"{ServerUrl!.Replace("http://", "ws://", StringComparison.Ordinal)}/.cratis/queries/ws");
        await socket.ConnectAsync(uri, timeout.Token);
        var buffer = new byte[8192];
        await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token); // Connected frame.

        var subscribe = new ObservableQueryHubMessage
        {
            Type = ObservableQueryHubMessageType.Subscribe,
            QueryId = "gated",
            Payload = new ObservableQuerySubscriptionRequest($"{typeof(GatedReadModel).FullName}.All")
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(subscribe, Json.Globals.JsonSerializerOptions);
        await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, timeout.Token);

        var receiving = socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
        try
        {
            await _gate.WaitForEntry();
            _receivedWhilePending = receiving.IsCompleted;
            _performedWhilePending = GatedReadModel.Performed;
        }
        finally
        {
            _gate.Release();
        }

        var frame = await receiving.WaitAsync(TimeSpan.FromSeconds(10));
        var message = JsonSerializer.Deserialize<ObservableQueryHubMessage>(
            new ReadOnlySpan<byte>(buffer, 0, frame.Count),
            Json.Globals.JsonSerializerOptions)!;
        _messageAfterRelease = message.Type;
    }

    [Fact] void should_not_emit_while_authorization_is_pending() => _receivedWhilePending.ShouldBeFalse();
    [Fact] void should_not_perform_the_query_before_authorization() => _performedWhilePending.ShouldEqual(_performedBefore);
    [Fact] void should_emit_after_authorization() => _messageAfterRelease.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
    [Fact] void should_only_perform_once() => GatedReadModel.Performed.ShouldEqual(_performedBefore + 1);
}
