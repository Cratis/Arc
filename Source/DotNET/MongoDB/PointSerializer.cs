// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="Point"/>.
/// </summary>
public class PointSerializer : SerializerBase<Point>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Point value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("type");
        context.Writer.WriteString("Point");
        context.Writer.WriteName("coordinates");
        context.Writer.WriteStartArray();
        context.Writer.WriteDouble(value.Longitude);
        context.Writer.WriteDouble(value.Latitude);
        context.Writer.WriteEndArray();
        context.Writer.WriteEndDocument();
    }

    /// <inheritdoc/>
    public override Point Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();

        double longitude = 0;
        double latitude = 0;

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = context.Reader.ReadName();
            if (name == "type")
            {
                var type = context.Reader.ReadString();

                // Validate that the type is "Point"
                if (type != "Point")
                {
                    throw new ArgumentException($"Expected type 'Point', but got '{type}'");
                }
            }
            else if (name == "coordinates")
            {
                context.Reader.ReadStartArray();
                longitude = context.Reader.ReadDouble();
                latitude = context.Reader.ReadDouble();
                context.Reader.ReadEndArray();
            }
            else
            {
                context.Reader.SkipValue();
            }
        }

        context.Reader.ReadEndDocument();

        return new Point(longitude, latitude);
    }
}