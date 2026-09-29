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
    /// Arc's own identity types resolve through <see cref="IdentityJsonSerializerContext"/>. Everything else - the
    /// application's identity details, a type known only at runtime - resolves through the resolver the application
    /// configured, or through reflection as before when it configured none.
    /// </remarks>
    public static JsonSerializerOptions CreateFrom(JsonSerializerOptions arcOptions, Func<IJsonTypeInfoResolver> createReflectionResolverForDetails)
    {
        var options = new JsonSerializerOptions(arcOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        var detailsResolver = options.TypeInfoResolver
            ?? (JsonSerializer.IsReflectionEnabledByDefault ? createReflectionResolverForDetails() : null);
        options.TypeInfoResolver = detailsResolver is null
            ? IdentityJsonSerializerContext.Default
            : JsonTypeInfoResolver.Combine(IdentityJsonSerializerContext.Default, detailsResolver);
        options.MakeReadOnly();
        return options;
    }
}
