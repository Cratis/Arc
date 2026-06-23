// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.MongoDB.for_JsonObjectBsonSerializer.given;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_JsonObjectBsonSerializer.when_deserializing;

public class a_bson_document : a_json_object_bson_serializer
{
    JsonObject _result;

    void Because() => _result = Deserialize(new BsonDocument { { "name", "test" }, { "count", 42 } });

    [Fact] void should_contain_the_name_property() => _result["name"]!.GetValue<string>().ShouldEqual("test");
    [Fact] void should_contain_the_count_property() => _result["count"]!.GetValue<int>().ShouldEqual(42);
}
