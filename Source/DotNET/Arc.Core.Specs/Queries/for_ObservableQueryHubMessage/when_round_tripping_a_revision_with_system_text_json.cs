// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_ObservableQueryHubMessage;

public class when_round_tripping_a_revision_with_system_text_json : Specification
{
    readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    ObservableQueryHubMessage _result;
    string _json;

    void Because()
    {
        var message = ObservableQueryHubMessage.CreateUnauthorized("query-a");
        typeof(ObservableQueryHubMessage).GetProperty("Revision")!.SetValue(message, 42L);
        _json = JsonSerializer.Serialize(message, _options);
        _result = JsonSerializer.Deserialize<ObservableQueryHubMessage>(_json, _options)!;
    }

    [Fact] void should_serialize_the_revision_as_a_number() => _json.ShouldContain("\"revision\":42");
    [Fact] void should_deserialize_the_revision() =>
        typeof(ObservableQueryHubMessage).GetProperty("Revision")!.GetValue(_result).ShouldEqual(42L);
    [Fact] void should_deserialize_the_query_id() => _result.QueryId.ShouldEqual("query-a");
    [Fact] void should_deserialize_the_message_type() => _result.Type.ShouldEqual(ObservableQueryHubMessageType.Unauthorized);
}
