// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Exercises authorization on the live ASP.NET Core WebSocket hub rather than a substituted query pipeline.
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_subscribing_to_policy_query : given.a_scenario_web_application
{
    ObservableQueryHubMessageType _deniedType;
    ObservableQueryHubMessageType _allowedType;
    int _performedBefore;

    void Establish() => _performedBefore = PolicyProtectedReadModel.Performed;

    async Task Because()
    {
        _deniedType = await Subscribe("Default");
        _allowedType = await Subscribe("Special");
    }

    [Fact] void should_reject_the_default_identity() => _deniedType.ShouldEqual(ObservableQueryHubMessageType.Unauthorized);
    [Fact] void should_admit_the_selected_identity() => _allowedType.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
    [Fact] void should_only_execute_the_admitted_query() => PolicyProtectedReadModel.Performed.ShouldEqual(_performedBefore + 1);
    [Fact] void should_execute_as_the_selected_identity() => PolicyProtectedReadModel.LastHttpCaller.ShouldEqual("Special");

    async Task<ObservableQueryHubMessageType> Subscribe(string scheme)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader($"X-{scheme}", "active");
        if (scheme == "Special")
        {
            socket.Options.SetRequestHeader("X-Default", "active");
        }
        var uri = new Uri($"{ServerUrl!.Replace("http://", "ws://", StringComparison.Ordinal)}/.cratis/queries/ws");
        await socket.ConnectAsync(uri, timeout.Token);

        var subscribe = new ObservableQueryHubMessage
        {
            Type = ObservableQueryHubMessageType.Subscribe,
            QueryId = "policy-test",
            Payload = new ObservableQuerySubscriptionRequest($"{typeof(PolicyProtectedReadModel).FullName}.All")
        };
        var payload = JsonSerializer.SerializeToUtf8Bytes(subscribe, Json.Globals.JsonSerializerOptions);
        await socket.SendAsync(new ArraySegment<byte>(payload), System.Net.WebSockets.WebSocketMessageType.Text, true, timeout.Token);

        var buffer = new byte[8192];
        while (true)
        {
            var frame = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
            var message = JsonSerializer.Deserialize<ObservableQueryHubMessage>(
                new ReadOnlySpan<byte>(buffer, 0, frame.Count),
                Json.Globals.JsonSerializerOptions)!;
            if (message.Type is ObservableQueryHubMessageType.QueryResult or ObservableQueryHubMessageType.Unauthorized or ObservableQueryHubMessageType.Error)
            {
                return message.Type;
            }
        }
    }
}
