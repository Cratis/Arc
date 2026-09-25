// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_subscription_policy_resolution_fails_at_runtime : given.a_scenario_web_application
{
    [Theory]
    [InlineData("X-Unknown-Runtime-Policy", ObservableQueryHubMessageType.Unauthorized, HttpStatusCode.Unauthorized)]
    [InlineData("X-Throw-Runtime-Policy", ObservableQueryHubMessageType.Error, HttpStatusCode.OK)]
    public async Task should_acknowledge_and_release_the_reserved_sse_subscription(string failureHeader, ObservableQueryHubMessageType expected, HttpStatusCode expectedStatus)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var open = new HttpRequestMessage(HttpMethod.Get, ObservableQueryDemultiplexerEndpointMapper.SseRoute);
        open.Headers.Add("X-Default", "active");
        using var response = await HttpClient!.SendAsync(open, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var connected = await ReadMessage(reader, timeout.Token);
        var connectionId = ((JsonElement)connected.Payload!).GetString()!;
        var subscription = new ObservableQuerySSESubscribeRequest(connectionId, "failed", new ObservableQuerySubscriptionRequest($"{typeof(PolicyProtectedReadModel).FullName}.All"));
        using var request = new HttpRequestMessage(HttpMethod.Post, ObservableQueryDemultiplexerEndpointMapper.SseSubscribeRoute)
        {
            Content = new StringContent(JsonSerializer.Serialize(subscription, Json.Globals.JsonSerializerOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Default", "active");
        request.Headers.Add("X-Special", "active");
        request.Headers.Add(failureHeader, "yes");
        using var reply = await HttpClient.SendAsync(request, timeout.Token);
        reply.StatusCode.ShouldEqual(expectedStatus);
        var acknowledgement = await ReadMessage(reader, timeout.Token);
        acknowledgement.Type.ShouldEqual(expected);
        Host!.Services.GetRequiredService<IQueryHealthTracker>().GetAllConnectionHealth()
            .SelectMany(connection => connection.Subscriptions)
            .Any(item => item.SubscriptionId == "failed").ShouldBeFalse();
    }

    [Theory]
    [InlineData("X-Unknown-Runtime-Policy")]
    [InlineData("X-Throw-Runtime-Policy")]
    public async Task should_not_expose_policy_provider_details_in_the_serialized_command_result(string failureHeader)
    {
        LoadCommandProxy<for_Commands.ModelBound.PolicyProtectedCommand>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
        HttpClient.DefaultRequestHeaders.Add(failureHeader, "yes");
        Host!.Services.GetRequiredService<IOptions<ArcOptions>>().Value.ExposeExceptionDetails.ShouldBeFalse();
        var result = (await Bridge!.ExecuteCommandViaProxyAsync<object>(new for_Commands.ModelBound.PolicyProtectedCommand())).Result;
        result.IsSuccess.ShouldBeFalse();
        var serialized = JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("ActiveSubscription");
        serialized.ShouldNotContain("SensitivePolicyProviderType");
        serialized.ShouldNotContain("Special are unavailable");
        result.AuthorizationFailureReason.ShouldBeEmpty();
    }

    static async Task<ObservableQueryHubMessage> ReadMessage(StreamReader reader, CancellationToken token)
    {
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<ObservableQueryHubMessage>(line["data: ".Length..], Json.Globals.JsonSerializerOptions)!;
            }
        }

        throw new InvalidOperationException("The SSE subscription ended before its control acknowledgement.");
    }
}
