// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// A real SSE GET and two live subscribe POSTs with different scheme credentials.
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_subscribing_to_policy_query_over_sse : given.a_scenario_web_application
{
    HttpStatusCode _deniedStatus;
    HttpStatusCode _allowedStatus;
    ObservableQueryHubMessageType _deniedMessage;
    ObservableQueryHubMessageType _allowedMessage;
    int _performedBefore;

    void Establish()
    {
        _performedBefore = PolicyProtectedReadModel.Performed;
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var open = new HttpRequestMessage(HttpMethod.Get, ObservableQueryDemultiplexerEndpointMapper.SseRoute);
        using var response = await HttpClient!.SendAsync(open, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var connected = await ReadMessage(reader, timeout.Token);
        var connectionId = ((JsonElement)connected.Payload!).GetString()!;

        _deniedStatus = await Subscribe(connectionId, "denied", false, timeout.Token);
        _deniedMessage = (await ReadMessage(reader, timeout.Token)).Type;
        _allowedStatus = await Subscribe(connectionId, "allowed", true, timeout.Token);
        _allowedMessage = (await ReadMessage(reader, timeout.Token)).Type;
    }

    [Fact] void should_deny_a_subscribe_post_without_the_selected_scheme() => _deniedMessage.ShouldEqual(ObservableQueryHubMessageType.Unauthorized);
    [Fact] void should_return_unauthorized_for_the_denied_post() => _deniedStatus.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_authorize_the_subscribe_post_with_the_selected_scheme() => _allowedMessage.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
    [Fact] void should_return_success_for_the_selected_post() => _allowedStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_run_only_the_selected_query() => PolicyProtectedReadModel.Performed.ShouldEqual(_performedBefore + 1);
    [Fact] void should_use_the_selected_principal_inside_the_query() => PolicyProtectedReadModel.LastHttpCaller.ShouldEqual("Special");

    async Task<HttpStatusCode> Subscribe(string connectionId, string queryId, bool selectedScheme, CancellationToken cancellationToken)
    {
        var subscription = new ObservableQuerySSESubscribeRequest(
            connectionId,
            queryId,
            new ObservableQuerySubscriptionRequest($"{typeof(PolicyProtectedReadModel).FullName}.All"));
        using var request = new HttpRequestMessage(HttpMethod.Post, ObservableQueryDemultiplexerEndpointMapper.SseSubscribeRoute)
        {
            Content = new StringContent(JsonSerializer.Serialize(subscription, Json.Globals.JsonSerializerOptions), Encoding.UTF8, "application/json")
        };
        if (selectedScheme)
        {
            request.Headers.Add("X-Special", "active");
        }

        using var response = await HttpClient!.SendAsync(request, cancellationToken);
        return response.StatusCode;
    }

    static async Task<ObservableQueryHubMessage> ReadMessage(StreamReader reader, CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<ObservableQueryHubMessage>(
                    line["data: ".Length..],
                    Json.Globals.JsonSerializerOptions)!;
            }
        }

        throw new InvalidOperationException("SSE stream closed before an Arc hub message arrived.");
    }
}
