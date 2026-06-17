// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Concepts;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a <see cref="IBsonSerializer{T}"/> for <see cref="ConceptAs{T}"/> types.
/// </summary>
/// <typeparam name="T">Type of concept.</typeparam>
public class ConceptSerializer<T> : IBsonSerializer<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConceptSerializer{T}"/> class.
    /// </summary>
    public ConceptSerializer()
    {
        ValueType = typeof(T);

        if (!ValueType.IsConcept())
            throw new TypeIsNotAConcept(ValueType);
    }

    /// <inheritdoc/>
    public Type ValueType { get; }

    /// <inheritdoc/>
    public T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;

        var actualType = args.NominalType;
        var bsonType = bsonReader.GetCurrentBsonType();

        var valueType = actualType.GetConceptValueType();

        object value;

        // It should be a Concept object
        if (bsonType == BsonType.Document)
        {
            bsonReader.ReadStartDocument();
            var keyName = bsonReader.ReadName(Utf8NameDecoder.Instance);
            if (keyName == "Value" || keyName == "value")
            {
                value = GetDeserializedValue(context, args, valueType, ref bsonReader);
                bsonReader.ReadEndDocument();
            }
            else
            {
                throw new MissingValueKeyInConcept();
            }
        }
        else
        {
            value = GetDeserializedValue(context, args, valueType, ref bsonReader);
        }

        if (value is null)
        {
            return default!;
        }

        return (T)ConceptFactory.CreateConceptInstance(ValueType, value);
    }

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        var bsonWriter = context.Writer;
        if (value is null)
        {
            bsonWriter.WriteNull();
            return;
        }

        var underlyingValue = value.GetConceptValue();
        var nominalType = args.NominalType;
        var underlyingValueType = nominalType.GetConceptValueType();

        if (underlyingValueType == typeof(Guid))
        {
            var guid = (Guid)underlyingValue;
            bsonWriter.WriteBinaryData(new BsonBinaryData(guid, GuidRepresentation.Standard));
        }
        else if (underlyingValueType == typeof(double))
        {
            bsonWriter.WriteDouble((double)underlyingValue);
        }
        else if (underlyingValueType == typeof(float))
        {
            bsonWriter.WriteDouble((double)underlyingValue);
        }
        else if (underlyingValueType == typeof(int))
        {
            bsonWriter.WriteInt32((int)underlyingValue);
        }
        else if (underlyingValueType == typeof(uint))
        {
            bsonWriter.WriteInt64((uint)underlyingValue);
        }
        else if (underlyingValueType == typeof(long))
        {
            bsonWriter.WriteInt64((long)underlyingValue);
        }
        else if (underlyingValueType == typeof(ulong))
        {
            bsonWriter.WriteDecimal128((ulong)underlyingValue);
        }
        else if (underlyingValueType == typeof(bool))
        {
            bsonWriter.WriteBoolean((bool)underlyingValue);
        }
        else if (underlyingValueType == typeof(string))
        {
            bsonWriter.WriteString((string)(underlyingValue ?? string.Empty));
        }
        else if (underlyingValueType == typeof(decimal))
        {
            bsonWriter.WriteDecimal128((decimal)underlyingValue);
        }
        else if (underlyingValueType == typeof(DateTime))
        {
            var dateTime = (DateTime)underlyingValue;
            bsonWriter.WriteDateTime(dateTime.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond);
        }
        else if (underlyingValueType == typeof(DateTimeOffset))
        {
            var serializer = new DateTimeOffsetSupportingBsonDateTimeSerializer();
            serializer.Serialize(context, args, (DateTimeOffset)underlyingValue);
        }
        else if (underlyingValueType == typeof(DateOnly))
        {
            var serializer = new DateOnlySerializer();
            serializer.Serialize(context, args, (DateOnly)underlyingValue);
        }
        else if (underlyingValueType == typeof(TimeOnly))
        {
            var serializer = new TimeOnlySerializer();
            serializer.Serialize(context, args, (TimeOnly)underlyingValue);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        Serialize(context, args, (object)value!);
    }

    /// <inheritdoc/>
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args)!;

    /// <summary>
    /// Reads the numeric value at the reader's current position, tolerating any BSON numeric representation.
    /// </summary>
    /// <param name="bsonReader">The <see cref="IBsonReader"/> to read from.</param>
    /// <returns>The numeric value read, boxed as the CLR type matching its stored BSON representation.</returns>
    /// <exception cref="UnableToDeserializeValueForConcept">Thrown when the stored BSON type is not numeric.</exception>
    /// <remarks>
    /// Values that flow through JSON-based pipelines (for example Chronicle projections that materialize event
    /// content via <c>ExpandoObject</c>) lose the distinction between integral and floating point numbers and
    /// are persisted as <see cref="BsonType.Double"/>. Reading them back with a representation-specific reader
    /// (for example <c>ReadInt32</c>) throws. Reading according to the actual stored BSON type and letting the
    /// caller convert keeps a concept deserializable regardless of how it was stored.
    /// </remarks>
    static object ReadNumeric(ref IBsonReader bsonReader)
    {
        var bsonType = bsonReader.GetCurrentBsonType();
        return bsonType switch
        {
            BsonType.Int32 => bsonReader.ReadInt32(),
            BsonType.Int64 => bsonReader.ReadInt64(),
            BsonType.Double => bsonReader.ReadDouble(),
            BsonType.Decimal128 => (decimal)bsonReader.ReadDecimal128(),
            BsonType.String => double.Parse(bsonReader.ReadString(), NumberStyles.Any, CultureInfo.InvariantCulture),
            _ => throw new UnableToDeserializeValueForConcept(typeof(object))
        };
    }

    object GetDeserializedValue(BsonDeserializationContext context, BsonDeserializationArgs args, Type valueType, ref IBsonReader bsonReader)
    {
        var bsonType = bsonReader.CurrentBsonType;
        if (bsonType == BsonType.Null)
        {
            bsonReader.ReadNull();
            return null!;
        }

        if (valueType == typeof(Guid))
        {
            if (bsonReader.GetCurrentBsonType() == BsonType.String)
            {
                return Guid.Parse(bsonReader.ReadString());
            }
            var binaryData = bsonReader.ReadBinaryData();
            return binaryData.ToGuid();
        }

        if (valueType == typeof(double))
        {
            return Convert.ToDouble(ReadNumeric(ref bsonReader));
        }

        if (valueType == typeof(float))
        {
            return Convert.ToSingle(ReadNumeric(ref bsonReader));
        }

        if (valueType == typeof(int))
        {
            return Convert.ToInt32(ReadNumeric(ref bsonReader));
        }

        if (valueType == typeof(uint))
        {
            return Convert.ToUInt32(ReadNumeric(ref bsonReader));
        }

        if (valueType == typeof(long))
        {
            return Convert.ToInt64(ReadNumeric(ref bsonReader));
        }

        if (valueType == typeof(ulong))
        {
            return Convert.ToUInt64(ReadNumeric(ref bsonReader));
        }

        if (valueType == typeof(bool))
        {
            return bsonReader.ReadBoolean();
        }

        if (valueType == typeof(string))
        {
            return bsonReader.ReadString();
        }

        if (valueType == typeof(decimal))
        {
            return Convert.ToDecimal(ReadNumeric(ref bsonReader));
        }

        if (valueType == typeof(DateTime))
        {
            var dateTimeValue = bsonReader.ReadDateTime();
            return DateTimeOffset.FromUnixTimeMilliseconds(dateTimeValue).DateTime;
        }

        if (valueType == typeof(DateTimeOffset))
        {
            var serializer = new DateTimeOffsetSupportingBsonDateTimeSerializer();
            return serializer.Deserialize(context, args);
        }

        if (valueType == typeof(DateOnly))
        {
            var serializer = new DateOnlySerializer();
            return serializer.Deserialize(context, args);
        }

        if (valueType == typeof(TimeOnly))
        {
            var serializer = new TimeOnlySerializer();
            return serializer.Deserialize(context, args);
        }

        throw new UnableToDeserializeValueForConcept(valueType);
    }
}
