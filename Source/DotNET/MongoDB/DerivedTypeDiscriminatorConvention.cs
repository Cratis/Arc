// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a MongoDB discriminator convention for handling types that have <see cref="DerivedTypeAttribute"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DerivedTypeDiscriminatorConvention"/> class.
/// </remarks>
/// <param name="derivedTypes"><see cref="IDerivedTypes"/> in the system.</param>
public class DerivedTypeDiscriminatorConvention(IDerivedTypes derivedTypes) : IDiscriminatorConvention
{
    /// <summary>
    /// The name of the property used by serializer as a discriminator.
    /// </summary>
    public const string PropertyName = "_derivedTypeId";

    /// <inheritdoc/>
    public string ElementName => PropertyName;

    /// <inheritdoc/>
    public Type GetActualType(IBsonReader bsonReader, Type nominalType)
    {
        var bookmark = bsonReader.GetBookmark();
        bsonReader.ReadStartDocument();

        string? type = null;
        if (bsonReader.FindElement(ElementName))
        {
            type = bsonReader.ReadString();
        }

        bsonReader.ReturnToBookmark(bookmark);
        return type is null
            ? nominalType
            : derivedTypes.GetDerivedTypeFor(nominalType, new DerivedTypeId(Guid.Parse(type)));
    }

    /// <inheritdoc/>
    public BsonValue GetDiscriminator(Type nominalType, Type actualType)
    {
        var attribute = actualType.GetCustomAttribute<DerivedTypeAttribute>()!;
        return attribute.Identifier.ToString();
    }
}
