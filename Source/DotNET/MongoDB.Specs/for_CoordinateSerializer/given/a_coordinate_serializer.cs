// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_CoordinateSerializer.given;

public class a_coordinate_serializer : Specification
{
    protected CoordinateSerializer _serializer;

    void Establish() => _serializer = new CoordinateSerializer();

    protected BsonDocument Serialize(Coordinate value)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        var context = BsonSerializationContext.CreateRoot(writer);
        _serializer.Serialize(context, default, value);
        return document;
    }

    protected Coordinate Deserialize(BsonDocument document)
    {
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);
        return _serializer.Deserialize(context, default);
    }
}
