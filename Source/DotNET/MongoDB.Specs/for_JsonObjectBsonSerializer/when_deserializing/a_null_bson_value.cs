// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.MongoDB.for_JsonObjectBsonSerializer.given;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_JsonObjectBsonSerializer.when_deserializing;

public class a_null_bson_value : a_json_object_bson_serializer
{
    JsonObject _result;

    void Because() => _result = Deserialize(BsonNull.Value);

    [Fact] void should_return_an_empty_json_object() => _result.ShouldBeEmpty();
}
