// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_a_selected_command_publishes_to_another_identity_stream : given.a_scenario_web_application
{
    SelectedEmissionObservations _observations;
    EmissionObservation _fromCommand;

    void Establish()
    {
        PolicyProtectedStream.Reset();
        _observations = Host!.Services.GetRequiredService<SelectedEmissionObservations>();
        _observations.Reset();
        LoadCommandProxy<PublishToSelectedStream>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Default-Tenant", "tenant-A");
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var open = new HttpRequestMessage(HttpMethod.Get, ObservableQueryDemultiplexerEndpointMapper.SseRoute);
        using var response = await HttpClient!.SendAsync(open, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var connection = await ReadMessage(reader, timeout.Token);
        var connectionId = ((JsonElement)connection.Payload!).GetString()!;

        var subscription = new ObservableQuerySSESubscribeRequest(
            connectionId,
            "stream",
            new ObservableQuerySubscriptionRequest($"{typeof(PolicyProtectedStream).FullName}.Watch"));
        using (var post = new HttpRequestMessage(HttpMethod.Post, ObservableQueryDemultiplexerEndpointMapper.SseSubscribeRoute)
        {
            Content = new StringContent(JsonSerializer.Serialize(subscription, Json.Globals.JsonSerializerOptions), Encoding.UTF8, "application/json")
        })
        {
            post.Headers.Add("X-Special", "active");
            post.Headers.Add("X-Special-Tenant", "tenant-B");
            using var subscribeResponse = await HttpClient.SendAsync(post, timeout.Token);
            subscribeResponse.IsSuccessStatusCode.ShouldBeTrue();
        }

        (await ReadMessage(reader, timeout.Token)).Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
        _observations.Reset();

        HttpClient.DefaultRequestHeaders.Add("X-Other", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Other-Tenant", "tenant-C");
        var commandResponse = await Bridge!.ExecuteCommandViaProxyAsync<object>(new PublishToSelectedStream());
        commandResponse.Result!.IsAuthorized.ShouldBeTrue();
        PublishToSelectedStream.LastPublisher.ShouldEqual("Other");
        (await ReadMessage(reader, timeout.Token)).Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
        _fromCommand = await _observations.Next();
    }

    [Fact] void should_execute_the_producing_command_as_c() => PublishToSelectedStream.LastPublisher.ShouldEqual("Other");
    [Fact] void should_restore_c_in_the_producer_after_publishing() => PublishToSelectedStream.LastPublisherAfterEmit.ShouldEqual("Other");
    [Fact] void should_guard_the_subscriber_as_b() => _fromCommand.Principal.ShouldEqual("Special");
    [Fact] void should_restore_b_even_inside_c_command_flow() => _fromCommand.AmbientPrincipal.ShouldEqual("Special");
    [Fact] void should_retain_b_tenant() => _fromCommand.Tenant.ShouldEqual("tenant-B");
    [Fact] void should_suppress_the_command_native_request() => _fromCommand.NativePrincipal.ShouldBeNull();

    static async Task<ObservableQueryHubMessage> ReadMessage(StreamReader reader, CancellationToken token)
    {
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<ObservableQueryHubMessage>(line["data: ".Length..], Json.Globals.JsonSerializerOptions)!;
            }
        }

        throw new InvalidOperationException("SSE stream closed before an Arc message arrived.");
    }
}
