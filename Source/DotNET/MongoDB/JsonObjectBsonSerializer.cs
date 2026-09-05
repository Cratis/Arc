// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// MongoDB BSON serializer for <see cref="JsonObject"/>.
/// </summary>
/// <remarks>
/// Read models persisted through Arc.MongoDB are serialized with the MongoDB BSON serializers. The driver has no
/// built-in support for <see cref="JsonObject"/>, so without this it falls back to dictionary serialization and
/// fails on read because <see cref="JsonObject"/> has no parameterless constructor. This bridges BSON and
/// <see cref="JsonObject"/> by round-tripping through a <see cref="BsonDocument"/>.
/// </remarks>
public class JsonObjectBsonSerializer : SerializerBase<JsonObject>
{
    static readonly JsonWriterSettings _relaxedJson = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    /// <inheritdoc/>
    public override JsonObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.GetCurrentBsonType() == BsonType.Null)
        {
            context.Reader.ReadNull();
            return [];
        }

        var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
        return JsonNode.Parse(document.ToJson(_relaxedJson))!.AsObject();
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonObject value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        BsonDocumentSerializer.Instance.Serialize(context, args, BsonDocument.Parse(value.ToJsonString()));
    }
}
