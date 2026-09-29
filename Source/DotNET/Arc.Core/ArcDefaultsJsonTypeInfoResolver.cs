// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc;

/// <summary>
/// The single entry <see cref="JsonSerializerOptionsConfiguration.ConfigureArcDefaults"/> puts in the
/// <see cref="JsonSerializerOptions.TypeInfoResolverChain"/> of the options it configures.
/// </summary>
/// <param name="owner">The <see cref="JsonSerializerOptions"/> this resolver was added to.</param>
/// <param name="reflectionResolver">The reflection-based resolver to fall back to, or <see langword="null"/> when reflection-based serialization is disabled.</param>
/// <remarks>
/// <para>
/// Before Arc shipped source-generated metadata, the chain of the options it configured was empty, so a resolver an
/// application appended with <c>TypeInfoResolverChain.Add(...)</c> after Arc configured the options was the only resolver
/// and decided the contract of every type. Putting Arc's metadata and the reflection-based resolver directly in the
/// chain would put such a resolver behind them, where it is never consulted for any type they know, silently changing
/// the JSON. So Arc adds this one resolver instead, and it consults, in order:
/// </para>
/// <list type="number">
/// <item><description>
/// The resolvers that follow it in the chain of the options it was added to - the ones appended after Arc configured the
/// options - so they keep winning as they did.
/// </description></item>
/// <item><description>Arc's source-generated metadata for its own wire types, <see cref="ArcJsonSerializerContext"/>.</description></item>
/// <item><description>The reflection-based resolver, only when reflection-based serialization is enabled.</description></item>
/// </list>
/// <para>
/// Resolvers ahead of it in the chain, such as those added through <see cref="ArcOptions.AddJsonTypeInfoResolver"/>, are
/// consulted by the chain before it. The resolvers that follow it are read from the chain when a type is resolved, which
/// happens at the latest on first use; <see cref="JsonSerializerOptions"/> become read-only then, so the chain cannot change
/// afterwards, and they cache what they resolve. It reads the chain of the options it was added to, not of the options
/// passed in, so a copy of those options - such as the identity options, which add Arc's identity metadata behind
/// everything - sees the same order. It never consults itself.
/// </para>
/// </remarks>
internal sealed class ArcDefaultsJsonTypeInfoResolver(JsonSerializerOptions owner, IJsonTypeInfoResolver? reflectionResolver) : IJsonTypeInfoResolver
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> this resolver was added to.
    /// </summary>
    internal JsonSerializerOptions Owner => owner;

    /// <summary>
    /// Gets the reflection-based resolver it falls back to, or <see langword="null"/> when reflection-based serialization is disabled.
    /// </summary>
    internal IJsonTypeInfoResolver? ReflectionResolver => reflectionResolver;

    /// <inheritdoc/>
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var chain = owner.TypeInfoResolverChain;
        var index = chain.IndexOf(this);
        if (index >= 0)
        {
            for (var i = index + 1; i < chain.Count; i++)
            {
                if (!ReferenceEquals(chain[i], this) && chain[i].GetTypeInfo(type, options) is { } appended)
                {
                    return appended;
                }
            }
        }

        return ((IJsonTypeInfoResolver)ArcJsonSerializerContext.Default).GetTypeInfo(type, options)
            ?? reflectionResolver?.GetTypeInfo(type, options);
    }
}
