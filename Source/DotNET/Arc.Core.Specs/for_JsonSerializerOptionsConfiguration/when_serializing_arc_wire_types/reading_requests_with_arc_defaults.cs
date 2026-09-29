// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_serializing_arc_wire_types;

/// <summary>
/// The requests Arc reads - observable query subscriptions, the query body envelope and hub messages - must read into
/// the same values through Arc's source-generated metadata as they did through reflection.
/// </summary>
public class reading_requests_with_arc_defaults : Specification
{
    const string SubscriptionRequestJson = """{"queryName":"Orders.AllOrders","arguments":{"customer":"42","filter":null},"page":2,"pageSize":20,"sortBy":"number","sortDirection":"desc","transferMode":"delta"}""";
    const string SseSubscribeRequestJson = """{"connectionId":"connection-1","queryId":"query-1","request":""" + SubscriptionRequestJson + ""","revision":3}""";
    const string SseUnsubscribeRequestJson = """{"connectionId":"connection-1","queryId":"query-1"}""";
    const string QueryRequestEnvelopeJson = """{"arguments":{"customer":"42","count":3},"paging":{"page":1,"pageSize":25},"sorting":{"field":"number","direction":"asc"}}""";
    const string HubMessageJson = """{"type":"Subscribe","queryId":"query-1","revision":4,"payload":""" + SubscriptionRequestJson + "}";
    const string WebSocketMessageJson = """{"type":"Ping","timestamp":1234567890}""";

    JsonSerializerOptions _options;
    JsonSerializerOptions _optionsAsBefore;

    void Establish()
    {
        _options = new JsonSerializerOptions().ConfigureArcDefaults();
        _optionsAsBefore = representative_wire_values.OptionsAsBefore();
    }

    [Fact] void should_read_a_subscription_request_the_same() => ShouldReadTheSame<ObservableQuerySubscriptionRequest>(SubscriptionRequestJson);
    [Fact] void should_read_a_server_sent_events_subscribe_request_the_same() => ShouldReadTheSame<ObservableQuerySSESubscribeRequest>(SseSubscribeRequestJson);
    [Fact] void should_read_a_server_sent_events_unsubscribe_request_the_same() => ShouldReadTheSame<ObservableQuerySSEUnsubscribeRequest>(SseUnsubscribeRequestJson);
    [Fact] void should_read_a_query_request_envelope_the_same() => ShouldReadTheSame<QueryRequestEnvelope>(QueryRequestEnvelopeJson);
    [Fact] void should_read_a_hub_message_the_same() => ShouldReadTheSame<ObservableQueryHubMessage>(HubMessageJson);
    [Fact] void should_read_a_web_socket_message_the_same() => ShouldReadTheSame<WebSocketMessage>(WebSocketMessageJson);

    void ShouldReadTheSame<T>(string json)
    {
        var read = JsonSerializer.Deserialize<T>(json, _options);
        var readAsBefore = JsonSerializer.Deserialize<T>(json, _optionsAsBefore);

        // Written back through reflection alone, so a difference can only come from how the value was read.
        JsonSerializer.Serialize(read, _optionsAsBefore).ShouldEqual(JsonSerializer.Serialize(readAsBefore, _optionsAsBefore));
    }
}
