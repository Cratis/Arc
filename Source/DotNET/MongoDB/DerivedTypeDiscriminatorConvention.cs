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

        // When the nominal type has no registered derivatives it is already the concrete type (for example
        // a leaf derived type being deserialized after its discriminator was resolved one level up). The
        // discriminator element may still be present on the document, so only resolve a derived type when
        // the nominal type actually has derivatives to avoid throwing for an unknown target type.
        var actualType = type is null || !derivedTypes.HasDerivatives(nominalType)
            ? nominalType
            : derivedTypes.GetDerivedTypeFor(nominalType, type);

        // Guard against handing back a non-instantiable type. MongoDB constructs a DiscriminatedInterfaceSerializer
        // for interface (and abstract) nominal types; if GetActualType returns that same non-instantiable type, the
        // driver looks the discriminated serializer up again and calls GetActualType on the identical bytes, looping
        // until the stack overflows and the process is killed (SIGSEGV). A polymorphic value that reaches here without
        // resolving to a concrete type is malformed or unregistered — surface it as a catchable exception instead.
        if (actualType.IsInterface || actualType.IsAbstract)
        {
            throw new CannotResolveConcreteDerivedType(nominalType, type);
        }

        return actualType;
    }

    /// <inheritdoc/>
    public BsonValue GetDiscriminator(Type nominalType, Type actualType)
    {
        var attribute = actualType.GetCustomAttribute<DerivedTypeAttribute>()!;
        return attribute.Identifier.ToString();
    }
}
