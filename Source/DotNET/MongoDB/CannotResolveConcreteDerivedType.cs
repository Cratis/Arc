// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Exception that gets thrown when a polymorphic value cannot be resolved to a concrete (instantiable) derived type
/// during BSON deserialization.
/// </summary>
/// <remarks>
/// Returning a non-instantiable nominal type (an interface or abstract class) from the discriminator convention
/// makes MongoDB's <c>DiscriminatedInterfaceSerializer</c> look the same serializer up again and re-enter
/// resolution on the same bytes — an unbounded recursion that overflows the stack and terminates the process.
/// Failing loudly and catchably instead keeps the failure contained to the query that hit it.
/// </remarks>
/// <param name="nominalType">The nominal type being deserialized.</param>
/// <param name="discriminator">The discriminator value read from the document, if any.</param>
public class CannotResolveConcreteDerivedType(Type nominalType, string? discriminator)
    : Exception($"Cannot resolve a concrete derived type for '{nominalType.FullName}' from discriminator '{discriminator ?? "<none>"}'. Returning the non-instantiable nominal type would cause the discriminated serializer to recurse indefinitely.");
