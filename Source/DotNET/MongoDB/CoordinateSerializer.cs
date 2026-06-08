// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if COORDINATE_TYPE_AVAILABLE
// NOTE: This serializer is ready for when Cratis.Fundamentals includes the Coordinate type
// from Cratis.Geospatial namespace. Remove the #if/#endif when the type is available.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="Coordinate"/>.
/// </summary>
public class CoordinateSerializer : StructSerializerBase<Coordinate>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Coordinate value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("longitude");
        context.Writer.WriteDouble(value.Longitude);
        context.Writer.WriteName("latitude");
        context.Writer.WriteDouble(value.Latitude);
        context.Writer.WriteEndDocument();
    }

    /// <inheritdoc/>
    public override Coordinate Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();
        
        double longitude = 0;
        double latitude = 0;
        
        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = context.Reader.ReadName();
            if (name == "longitude")
            {
                longitude = context.Reader.ReadDouble();
            }
            else if (name == "latitude")
            {
                latitude = context.Reader.ReadDouble();
            }
            else
            {
                context.Reader.SkipValue();
            }
        }
        
        context.Reader.ReadEndDocument();
        
        return new Coordinate(longitude, latitude);
    }
}
#endif
