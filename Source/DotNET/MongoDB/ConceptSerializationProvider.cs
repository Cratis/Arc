// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
#if NET9_0_OR_GREATER
using System.Reflection;
using System.Runtime.CompilerServices;
#endif
using Cratis.Concepts;
using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for concepts.
/// </summary>
public class ConceptSerializationProvider : IBsonSerializationProvider
{
    /// <summary>
    /// Creates an instance of a serializer of the concept of the given type param T.
    /// </summary>
    /// <typeparam name="T">The Concept type.</typeparam>
    /// <returns><see cref="ConceptSerializer{T}"/> for the specific type.</returns>
    public static ConceptSerializer<T> CreateConceptSerializer<T>()
    {
        return new ConceptSerializer<T>();
    }

    /// <inheritdoc/>
    public IBsonSerializer GetSerializer(Type type)
    {
        if (type.IsConcept())
        {
#if NET9_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                return GetSerializerWithoutDynamicCode(type);
            }
#endif

            return GetSerializerThroughReflection(type);
        }

        return null!;
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// Gets the serializer of a concept without closing a generic method through reflection over dynamic code.
    /// </summary>
    /// <param name="type">The concept type.</param>
    /// <returns>The <see cref="IBsonSerializer"/> for the concept.</returns>
    internal static IBsonSerializer GetSerializerWithoutDynamicCode(Type type) =>
        (IBsonSerializer)typeof(ConceptSerializationProvider)
            .GetMethod(nameof(CreateReferenceConceptSerializer), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type)
            .Invoke(null, [])!;

    /// <summary>
    /// Creates the serializer of a concept. Concepts are record classes, so closing over a reference type needs no dynamic code.
    /// </summary>
    /// <typeparam name="T">The concept type.</typeparam>
    /// <returns>The <see cref="ConceptSerializer{T}"/> for the concept.</returns>
    static ConceptSerializer<T> CreateReferenceConceptSerializer<T>()
        where T : class => new();
#endif

    [RequiresDynamicCode("Closes the concept serializer over a concept type only known at runtime.")]
    IBsonSerializer GetSerializerThroughReflection(Type type)
    {
        var createConceptSerializerGenericMethod = GetType().GetMethod(nameof(CreateConceptSerializer))!.MakeGenericMethod(type);
        return (createConceptSerializerGenericMethod.Invoke(null, []) as IBsonSerializer)!;
    }
}
