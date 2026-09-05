// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Class map convention that registers the derived-type discriminator convention for every concrete
/// [DerivedType] as its class map is created.
/// </summary>
/// <remarks>
/// The target types (interfaces and base classes) get the convention registered up front, but the driver
/// resolves the discriminator convention per CLASS MAP when delegating from a discriminated interface
/// serializer to the concrete type's serializer — and it passes the original (interface) nominal type along.
/// Without this registration the concrete type's class map falls back to the driver's default "_t"
/// convention, which cannot see the "_derivedTypeId" element and answers with the interface nominal type,
/// bouncing deserialization back to the discriminated interface serializer in an unbounded loop that
/// overflows the stack. Registering the same convention for the concrete types keeps both sides symmetric:
/// writes emit "_derivedTypeId" and the re-check resolves to the type itself, terminating the delegation.
/// </remarks>
/// <param name="derivedTypes">The <see cref="IDerivedTypes"/> system for knowing which types are derived types.</param>
/// <param name="discriminatorConvention">The <see cref="IDiscriminatorConvention"/> to register for derived types.</param>
public class DerivedTypeClassMapConvention(IDerivedTypes derivedTypes, IDiscriminatorConvention discriminatorConvention) : ConventionBase, IClassMapConvention
{
    readonly ConcurrentDictionary<Type, bool> _registered = new();

    /// <inheritdoc/>
    public void Apply(BsonClassMap classMap)
    {
        var type = classMap.ClassType;

        // Types that also have derivatives of their own (an intermediate that is both a [DerivedType] and a
        // base of other [DerivedType]s) already got the convention registered up front as target types —
        // registering again would throw. Only the leaf derived types need the class-map-time registration.
        if (derivedTypes.IsDerivedType(type) && !derivedTypes.HasDerivatives(type) && _registered.TryAdd(type, true))
        {
            try
            {
                BsonSerializer.RegisterDiscriminatorConvention(type, discriminatorConvention);
            }
            catch (BsonSerializationException)
            {
                // The driver memoizes discriminator conventions per type as it walks class hierarchies during
                // lookups, and can have registered one for this type before its class map was created. That
                // memoized convention was inherited from a base type that carries ours, so the registration is
                // already correct — a duplicate here is fine to leave in place.
            }
        }
    }
}
