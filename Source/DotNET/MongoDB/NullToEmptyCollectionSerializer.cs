// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an <see cref="IBsonSerializer{TValue}"/> that deserializes a null collection as an empty one.
/// </summary>
/// <typeparam name="TCollection">Type of the collection member.</typeparam>
/// <param name="inner">The <see cref="IBsonSerializer{TValue}"/> doing the actual work.</param>
/// <param name="emptyValueFactory">Creates the empty collection to substitute for a stored null.</param>
/// <remarks>
/// <para>
/// A default value only covers a member whose element is <b>absent</b> from the document — the driver never invokes the
/// member's serializer at all in that case. An element that is present and holds null goes through the serializer, which
/// is why <see cref="ReadModelCollectionsNeverNullConvention"/> needs both halves to make good on its guarantee.
/// </para>
/// <para>
/// Writing is delegated untouched — a collection that is null in memory is still written as null. Only the read
/// direction is adjusted, which is what keeps this from changing the shape of anything already stored.
/// </para>
/// </remarks>
internal sealed class NullToEmptyCollectionSerializer<TCollection>(
    IBsonSerializer<TCollection> inner,
    Func<object> emptyValueFactory) : SerializerBase<TCollection>, IBsonArraySerializer
{
    /// <inheritdoc/>
    public override TCollection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.GetCurrentBsonType() == BsonType.Null)
        {
            context.Reader.ReadNull();
            return (TCollection)emptyValueFactory();
        }

        return inner.Deserialize(context, args);
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TCollection value) =>
        inner.Serialize(context, args, value);

    /// <inheritdoc/>
    /// <remarks>
    /// The LINQ provider asks a member's serializer for its item serialization info to translate expressions that reach
    /// into the collection — <c>Any()</c>, <c>Count</c>, element access. Arc's server-side paging runs on
    /// <c>AsQueryable()</c>, so failing to forward this would silently break query translation for every member this
    /// wraps.
    /// </remarks>
    public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
    {
        if (inner is IBsonArraySerializer arraySerializer)
        {
            return arraySerializer.TryGetItemSerializationInfo(out serializationInfo);
        }

        serializationInfo = null!;
        return false;
    }
}
