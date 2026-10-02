// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.Identity;

/// <summary>
/// Creates the <see cref="JsonSerializerOptions"/> the identity response serializes with.
/// </summary>
internal static class IdentityJsonSerializerOptions
{
    /// <summary>
    /// Create the identity serializer options from the options Arc is configured with.
    /// </summary>
    /// <param name="arcOptions">The <see cref="JsonSerializerOptions"/> Arc is configured with.</param>
    /// <returns>Read-only <see cref="JsonSerializerOptions"/> for identity serialization.</returns>
    /// <remarks>
    /// The resolver the Arc options carry - the chain <see cref="JsonSerializerOptionsConfiguration.ConfigureArcDefaults"/>
    /// composes, or one the application replaced it with - comes first, so its contracts, including any the application
    /// customizes for Arc's identity types, win as before, and <see cref="IdentityJsonSerializerContext"/> only answers
    /// for the identity types that resolver does not know, such as in a NativeAOT application whose source-generated
    /// context covers just its own identity details. When the options carry none, Arc's context comes first and
    /// everything it does not know - the application's identity details, a type known only at runtime - resolves
    /// through the reflection-based resolver <see cref="JsonSerializer"/> falls back to, when reflection is enabled.
    /// <para>
    /// When the resolver the Arc options carry holds Arc's own resolver, which consults the reflection-based resolver itself,
    /// that one is replaced by an Arc resolver for these options whose last resort is Arc's context behind the
    /// reflection-based resolver. Appending Arc's context to the chain instead would put it among the resolvers Arc's
    /// resolver consults first, ahead of reflection.
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions CreateFrom(JsonSerializerOptions arcOptions)
    {
        var options = new JsonSerializerOptions(arcOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        var resolvers = options.TypeInfoResolverChain.ToArray();
        var arcIndex = Array.FindIndex(resolvers, _ => _ is ArcDefaultsJsonTypeInfoResolver);
        if (arcIndex >= 0)
        {
            var fallback = ((ArcDefaultsJsonTypeInfoResolver)resolvers[arcIndex]).Fallback;
            resolvers[arcIndex] = new ArcDefaultsJsonTypeInfoResolver(
                options,
                fallback is null ? IdentityJsonSerializerContext.Default : JsonTypeInfoResolver.Combine(fallback, IdentityJsonSerializerContext.Default));
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(resolvers);
        }
        else if (options.TypeInfoResolver is { } appResolver)
        {
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(appResolver, IdentityJsonSerializerContext.Default);
        }
        else if (JsonSerializerOptionsConfiguration.ReflectionResolver is { } reflectionResolver)
        {
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(IdentityJsonSerializerContext.Default, reflectionResolver);
        }
        else
        {
            options.TypeInfoResolver = IdentityJsonSerializerContext.Default;
        }

        options.MakeReadOnly();
        return options;
    }
}
