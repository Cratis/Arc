// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer.given;

public class a_concept_serializer : Specification
{
    protected static T Deserialize<T>(BsonValue storedValue)
    {
        var document = new BsonDocument { { "value", storedValue } };
        using var reader = new BsonDocumentReader(document);
        reader.ReadStartDocument();
        reader.ReadName();

        var serializer = new ConceptSerializer<T>();
        var context = BsonDeserializationContext.CreateRoot(reader);
        var result = serializer.Deserialize(context, new BsonDeserializationArgs { NominalType = typeof(T) });

        reader.ReadEndDocument();
        return result;
    }

    protected static T Roundtrip<T>(T value)
    {
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
        {
            writer.WriteStartDocument();
            writer.WriteName("value");

            var serializer = new ConceptSerializer<T>();
            var context = BsonSerializationContext.CreateRoot(writer);
            serializer.Serialize(context, new BsonSerializationArgs { NominalType = typeof(T) }, value);

            writer.WriteEndDocument();
        }

        return Deserialize<T>(document["value"]);
    }
}
