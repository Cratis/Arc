// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_an_sse_stream_emits_after_subscribe_post_is_disposed : given.a_scenario_web_application
{
    SelectedEmissionObservations _observations;
    EmissionObservation _later;
    string _outerTenant;

    void Establish()
    {
        PolicyProtectedStream.Reset();
        _observations = Host!.Services.GetRequiredService<SelectedEmissionObservations>();
        _observations.Reset();
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
        var connected = await ReadMessage(reader, timeout.Token);
        var connectionId = ((JsonElement)connected.Payload!).GetString()!;
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
            post.Headers.Add("X-PreResolve-Tenant", "true");
            using var subscribeResponse = await HttpClient.SendAsync(post, timeout.Token);
            subscribeResponse.IsSuccessStatusCode.ShouldBeTrue();
        }

        var initial = await ReadMessage(reader, timeout.Token);
        initial.Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
        _observations.Reset();

        // The subscribe POST and its request scope are gone. Simulate a producer with a cached default tenant A.
        var accessor = Host!.Services.GetRequiredService<IHttpRequestContextAccessor>();
        var previous = accessor.Current;
        var simulatedProducer = Substitute.For<IHttpRequestContext>();
        simulatedProducer.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "tenant-A")], "Default")));
        accessor.Current = simulatedProducer;
        try
        {
            _outerTenant = Host.Services.GetRequiredService<ITenantIdAccessor>().Current.Value;
            PolicyProtectedStream.Emit();
            var laterFrame = await ReadMessage(reader, timeout.Token);
            laterFrame.Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
            _later = await _observations.Next();
        }
        finally
        {
            accessor.Current = previous;
        }
    }

    [Fact] void should_start_with_a_different_cached_tenant() => _outerTenant.ShouldEqual("tenant-A");
    [Fact] void should_preserve_the_selected_principal_after_the_post_is_disposed() => _later.Principal.ShouldEqual("Special");
    [Fact] void should_pin_the_selected_tenant_during_later_emissions() => _later.Tenant.ShouldEqual("tenant-B");
    [Fact] void should_resolve_guard_services_in_the_selected_tenant() => _later.ScopedTenant.ShouldEqual("tenant-B");
    [Fact] void should_restore_the_ambient_subscriber_principal() => _later.AmbientPrincipal.ShouldEqual("Special");
    [Fact] void should_not_expose_the_disposed_subscribe_request_natively() => _later.NativePrincipal.ShouldBeNull();

    static async Task<ObservableQueryHubMessage> ReadMessage(StreamReader reader, CancellationToken token)
    {
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<ObservableQueryHubMessage>(line["data: ".Length..], Json.Globals.JsonSerializerOptions)!;
            }
        }

        throw new InvalidOperationException("SSE stream closed before the expected query result.");
    }
}
