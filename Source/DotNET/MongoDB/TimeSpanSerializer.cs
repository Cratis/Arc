// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="TimeSpan"/>.
/// </summary>
public class TimeSpanSerializer : StructSerializerBase<TimeSpan>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeSpan value)
    {
        context.Writer.WriteString(value.ToString());
    }

    /// <inheritdoc/>
    public override TimeSpan Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return TimeSpan.Parse(value);
    }
}
