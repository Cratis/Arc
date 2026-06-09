// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="LineString"/>.
/// </summary>
public class LineStringSerializer : SerializerBase<LineString>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, LineString value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("type");
        context.Writer.WriteString("LineString");
        context.Writer.WriteName("coordinates");
        context.Writer.WriteStartArray();
        foreach (var point in value.Coordinates)
        {
            context.Writer.WriteStartArray();
            context.Writer.WriteDouble(point.Longitude);
            context.Writer.WriteDouble(point.Latitude);
            context.Writer.WriteEndArray();
        }
        context.Writer.WriteEndArray();
        context.Writer.WriteEndDocument();
    }

    /// <inheritdoc/>
    public override LineString Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();

        var coordinates = new List<Point>();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = context.Reader.ReadName();
            if (name == "type")
            {
                var type = context.Reader.ReadString();

                if (type != "LineString")
                {
                    throw new ArgumentException($"Expected type 'LineString', but got '{type}'");
                }
            }
            else if (name == "coordinates")
            {
                coordinates = ReadLineStringCoordinates(context.Reader);
            }
            else
            {
                context.Reader.SkipValue();
            }
        }

        context.Reader.ReadEndDocument();

        return new LineString(coordinates.ToArray());
    }

    static List<Point> ReadLineStringCoordinates(IBsonReader reader)
    {
        var points = new List<Point>();
        reader.ReadStartArray();

        try
        {
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                reader.ReadStartArray();
                var longitude = reader.ReadDouble();
                var latitude = reader.ReadDouble();
                reader.ReadEndArray();
                points.Add(new Point(longitude, latitude));
            }
        }
        catch (EndOfStreamException)
        {
        }

        reader.ReadEndArray();
        return points;
    }
}