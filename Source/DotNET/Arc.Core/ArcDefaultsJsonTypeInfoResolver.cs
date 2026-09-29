// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc;

/// <summary>
/// The single resolver <see cref="JsonSerializerOptionsConfiguration.ConfigureArcDefaults"/> puts at the end of the
/// resolver chain of the options it configures.
/// </summary>
/// <param name="owner">The <see cref="JsonSerializerOptions"/> this resolver was created for.</param>
/// <param name="fallback">
/// The resolver to consult last - the reflection-based resolver when reflection-based serialization is enabled - or
/// <see langword="null"/> for none.
/// </param>
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
/// The resolvers that follow it in the resolver chain it is consulted from - the ones appended after Arc configured the
/// options - so they keep winning as they did.
/// </description></item>
/// <item><description>Arc's source-generated metadata for its own wire types, <see cref="ArcJsonSerializerContext"/>.</description></item>
/// <item><description>The fallback, when there is one.</description></item>
/// </list>
/// <para>
/// Resolvers ahead of it in the chain, such as those added through <see cref="ArcOptions.AddJsonTypeInfoResolver"/>, are
/// consulted by the chain before it. When the application wraps the options' resolver, this resolver is no longer a
/// top-level entry of the chain, and resolvers added through <see cref="ArcOptions.AddJsonTypeInfoResolver"/> after that
/// are put ahead of the wrapper, so they are still consulted first. The resolvers that follow it are read from the chain of the options passed in when
/// that chain holds this resolver, so a resolver appended to a copy of the options is consulted for the copy. When it does
/// not - because the application wrapped the resolver, for instance with <c>WithAddedModifier</c> - they are read from the
/// chain of the options it was created for. The chain is read when a type is resolved, which happens at the latest on
/// first use; <see cref="JsonSerializerOptions"/> become read-only then, so the chain cannot change afterwards, and they
/// cache what they resolve.
/// </para>
/// <para>
/// A resolver that follows it can itself hold it, as <c>JsonTypeInfoResolver.Combine(options.TypeInfoResolver, other)</c>
/// does. When this resolver is consulted again for a type it is already resolving on the same thread, it skips the
/// resolvers that follow it and goes straight to Arc's metadata and the fallback, so that composition resolves in the
/// order it states rather than recursing forever.
/// </para>
/// </remarks>
internal sealed class ArcDefaultsJsonTypeInfoResolver(JsonSerializerOptions owner, IJsonTypeInfoResolver? fallback) : IJsonTypeInfoResolver
{
    [ThreadStatic]
    static HashSet<(ArcDefaultsJsonTypeInfoResolver Resolver, Type Type)>? _resolving;

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> this resolver was created for.
    /// </summary>
    internal JsonSerializerOptions Owner => owner;

    /// <summary>
    /// Gets the resolver it consults last, or <see langword="null"/> for none.
    /// </summary>
    internal IJsonTypeInfoResolver? Fallback => fallback;

    /// <inheritdoc/>
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var resolving = _resolving ??= [];
        var key = (this, type);
        if (resolving.Add(key))
        {
            try
            {
                if (GetTypeInfoFromAppendedResolvers(type, options) is { } appended)
                {
                    return appended;
                }
            }
            finally
            {
                resolving.Remove(key);
            }
        }

        return ((IJsonTypeInfoResolver)ArcJsonSerializerContext.Default).GetTypeInfo(type, options)
            ?? fallback?.GetTypeInfo(type, options);
    }

    JsonTypeInfo? GetTypeInfoFromAppendedResolvers(Type type, JsonSerializerOptions options)
    {
        var chain = options.TypeInfoResolverChain;
        var index = chain.IndexOf(this);
        if (index < 0 && !ReferenceEquals(options, owner))
        {
            chain = owner.TypeInfoResolverChain;
            index = chain.IndexOf(this);
        }

        if (index < 0)
        {
            return null;
        }

        for (var i = index + 1; i < chain.Count; i++)
        {
            if (!ReferenceEquals(chain[i], this) && chain[i].GetTypeInfo(type, options) is { } appended)
            {
                return appended;
            }
        }

        return null;
    }
}
