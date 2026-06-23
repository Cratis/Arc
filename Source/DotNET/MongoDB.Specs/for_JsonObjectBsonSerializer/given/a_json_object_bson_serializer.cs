// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_JsonObjectBsonSerializer.given;

public class a_json_object_bson_serializer : Specification
{
    protected static JsonObject Deserialize(BsonValue storedValue)
    {
        var document = new BsonDocument { { "value", storedValue } };
        using var reader = new BsonDocumentReader(document);
        reader.ReadStartDocument();
        reader.ReadName();

        var serializer = new JsonObjectBsonSerializer();
        var context = BsonDeserializationContext.CreateRoot(reader);
        var result = serializer.Deserialize(context, new BsonDeserializationArgs { NominalType = typeof(JsonObject) });

        reader.ReadEndDocument();
        return result;
    }

    protected static BsonDocument Serialize(JsonObject value)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        var serializer = new JsonObjectBsonSerializer();
        var context = BsonSerializationContext.CreateRoot(writer);
        serializer.Serialize(context, new BsonSerializationArgs { NominalType = typeof(JsonObject) }, value);
        return document;
    }
}
