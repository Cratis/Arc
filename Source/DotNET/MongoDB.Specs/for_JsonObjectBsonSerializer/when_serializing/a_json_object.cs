// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.MongoDB.for_JsonObjectBsonSerializer.given;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_JsonObjectBsonSerializer.when_serializing;

public class a_json_object : a_json_object_bson_serializer
{
    BsonDocument _result;
    JsonObject _input;

    void Establish() => _input = new JsonObject { { "name", "test" }, { "count", 42 } };

    void Because() => _result = Serialize(_input);

    [Fact] void should_contain_the_name_property() => _result["name"].AsString.ShouldEqual("test");
    [Fact] void should_contain_the_count_property() => _result["count"].AsInt32.ShouldEqual(42);
}
