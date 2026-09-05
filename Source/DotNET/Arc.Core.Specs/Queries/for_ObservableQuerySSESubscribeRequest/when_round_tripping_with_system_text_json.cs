// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_ObservableQuerySSESubscribeRequest;

public class when_round_tripping_with_system_text_json : Specification
{
    readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    ObservableQuerySSESubscribeRequest _result;
    string _json;

    void Because()
    {
        var request = new ObservableQuerySSESubscribeRequest(
            "connection-a",
            "query-a",
            new ObservableQuerySubscriptionRequest("Namespace.Query"));
        typeof(ObservableQuerySSESubscribeRequest).GetProperty("Revision")!.SetValue(request, 42L);
        _json = JsonSerializer.Serialize(request, _options);
        _result = JsonSerializer.Deserialize<ObservableQuerySSESubscribeRequest>(_json, _options)!;
    }

    [Fact] void should_serialize_the_revision_as_a_number() => _json.ShouldContain("\"revision\":42");
    [Fact] void should_deserialize_the_connection_id() => _result.ConnectionId.ShouldEqual("connection-a");
    [Fact] void should_deserialize_the_query_id() => _result.QueryId.ShouldEqual("query-a");
    [Fact] void should_deserialize_the_request() => _result.Request.QueryName.ShouldEqual("Namespace.Query");
    [Fact] void should_deserialize_the_revision() =>
        typeof(ObservableQuerySSESubscribeRequest).GetProperty("Revision")!.GetValue(_result).ShouldEqual(42L);
}
