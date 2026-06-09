// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="Polygon"/>.
/// </summary>
public class PolygonSerializer : SerializerBase<Polygon>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Polygon value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("type");
        context.Writer.WriteString("Polygon");
        context.Writer.WriteName("coordinates");
        context.Writer.WriteStartArray();

        WriteLinearRing(context.Writer, value.Shell);

        foreach (var hole in value.Holes)
        {
            WriteLinearRing(context.Writer, hole);
        }

        context.Writer.WriteEndArray();
        context.Writer.WriteEndDocument();
    }

    /// <inheritdoc/>
    public override Polygon Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();

        var rings = new List<LinearRing>();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = context.Reader.ReadName();
            if (name == "type")
            {
                var type = context.Reader.ReadString();

                if (type != "Polygon")
                {
                    throw new ArgumentException($"Expected type 'Polygon', but got '{type}'");
                }
            }
            else if (name == "coordinates")
            {
                rings = ReadPolygonCoordinates(context.Reader);
            }
            else
            {
                context.Reader.SkipValue();
            }
        }

        context.Reader.ReadEndDocument();

        if (rings.Count == 0)
        {
            throw new ArgumentException("Polygon must have at least one ring (shell)");
        }

        var shell = rings[0];
        var holes = rings.Count > 1 ? rings.Skip(1).ToArray() : [];
        return new Polygon(shell, holes);
    }

    static void WriteLinearRing(IBsonWriter writer, LinearRing ring)
    {
        writer.WriteStartArray();
        foreach (var point in ring.Coordinates)
        {
            writer.WriteStartArray();
            writer.WriteDouble(point.Longitude);
            writer.WriteDouble(point.Latitude);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }

    static List<LinearRing> ReadPolygonCoordinates(IBsonReader reader)
    {
        var rings = new List<LinearRing>();
        reader.ReadStartArray();

        try
        {
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                rings.Add(ReadLinearRing(reader));
            }
        }
        catch (EndOfStreamException)
        {
        }

        reader.ReadEndArray();
        return rings;
    }

    static LinearRing ReadLinearRing(IBsonReader reader)
    {
        var points = new List<Point>();

        reader.ReadStartArray();

        try
        {
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var longitude = reader.ReadDouble();
                var latitude = reader.ReadDouble();
                points.Add(new Point(longitude, latitude));
            }
        }
        catch (EndOfStreamException)
        {
        }

        reader.ReadEndArray();

        if (points.Count < 4)
        {
            throw new ArgumentException("LinearRing must have at least 4 points");
        }

        var firstPoint = points[0];
        var lastPoint = points[^1];
        if (!AreNearlyEqual(firstPoint.Longitude, lastPoint.Longitude) || !AreNearlyEqual(firstPoint.Latitude, lastPoint.Latitude))
        {
            throw new ArgumentException("LinearRing must be closed (first and last points must be identical)");
        }

        return new LinearRing([.. points]);
    }

    static bool AreNearlyEqual(double a, double b, double epsilon = 1e-9) => Math.Abs(a - b) <= epsilon;
}