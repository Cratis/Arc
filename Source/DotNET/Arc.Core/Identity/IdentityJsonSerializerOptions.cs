// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.Identity;

/// <summary>
/// Creates the <see cref="JsonSerializerOptions"/> the identity cookie and the identity response serialize with.
/// </summary>
internal static class IdentityJsonSerializerOptions
{
    /// <summary>
    /// Create the identity serializer options from the options Arc is configured with.
    /// </summary>
    /// <param name="arcOptions">The <see cref="JsonSerializerOptions"/> Arc is configured with.</param>
    /// <param name="createReflectionResolverForDetails">
    /// Creates the reflection-based resolver for the application's identity details. Only called when the application
    /// configured no resolver of its own and reflection-based serialization is enabled - which is what
    /// <see cref="JsonSerializer"/> itself falls back to in that case.
    /// </param>
    /// <returns>Read-only <see cref="JsonSerializerOptions"/> for identity serialization.</returns>
    /// <remarks>
    /// When the application configured a resolver it comes first in the chain, so its contracts - including any it
    /// customizes for Arc's identity types - win as before, and <see cref="IdentityJsonSerializerContext"/> only
    /// answers for the identity types that resolver does not know, such as in a NativeAOT application whose
    /// source-generated context covers just its own identity details. When the application configured none, Arc's
    /// context comes first and everything it does not know - the application's identity details, a type known only at
    /// runtime - resolves through reflection as before.
    /// </remarks>
    public static JsonSerializerOptions CreateFrom(JsonSerializerOptions arcOptions, Func<IJsonTypeInfoResolver> createReflectionResolverForDetails)
    {
        var options = new JsonSerializerOptions(arcOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        if (options.TypeInfoResolver is { } appResolver)
        {
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(appResolver, IdentityJsonSerializerContext.Default);
        }
        else if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(IdentityJsonSerializerContext.Default, createReflectionResolverForDetails());
        }
        else
        {
            options.TypeInfoResolver = IdentityJsonSerializerContext.Default;
        }

        options.MakeReadOnly();
        return options;
    }
}
