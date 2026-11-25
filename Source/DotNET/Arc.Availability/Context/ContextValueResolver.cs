// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// The resolver of one request. Provided values resolve lazily, once, and the result is reused for the whole request.
/// </summary>
/// <param name="catalog">The declarations of the model.</param>
/// <param name="scope">The scope the request runs in.</param>
/// <param name="ambient">The values the runtime supplies.</param>
/// <param name="audiences">Optional lazy source of the effective audiences.</param>
public class ContextValueResolver(
    ContextValueCatalog catalog,
    ContextScope scope,
    IReadOnlyDictionary<string, object?> ambient,
    Func<CancellationToken, ValueTask<EffectiveAudiences?>>? audiences = null) : IContextValueResolver
{
    const string Prefix = "$context.";

    readonly Lock _lock = new();
    readonly Dictionary<string, Lazy<Task<ContextValueOutcome>>> _resolved = [];

    /// <summary>
    /// Gets the names of the provided values that have been resolved so far in this request.
    /// </summary>
    public IReadOnlyCollection<string> ResolvedNames
    {
        get
        {
            lock (_lock)
            {
                return [.. _resolved.Keys];
            }
        }
    }

    /// <inheritdoc/>
    public ValueTask<ContextValueOutcome> Resolve(string path, CancellationToken cancellationToken = default) =>
        ResolvePath(path, [], cancellationToken);

    /// <summary>
    /// Gets the effective audiences of the request.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The audiences, or <see langword="null"/> when the request has none resolved, which is unknown.</returns>
    public async ValueTask<EffectiveAudiences?> GetAudiences(CancellationToken cancellationToken = default)
    {
        var outcome = await ResolveValue(ReservedContextNames.Audiences, [], cancellationToken);
        return outcome.Value as EffectiveAudiences;
    }

    static ContextValueOutcome Navigate(ContextValueOutcome start, IEnumerable<string> segments)
    {
        var current = start;
        foreach (var segment in segments)
        {
            if (!current.IsPresent)
            {
                return ContextValueOutcome.Absent;
            }

            current = Member(current.Value!, segment);
        }

        return current;
    }

    static ContextValueOutcome Member(object instance, string name)
    {
        if (instance is IReadOnlyDictionary<string, object?> readOnly)
        {
            return readOnly.TryGetValue(name, out var found) ? ContextValueOutcome.Present(found) : ContextValueOutcome.Absent;
        }

        if (instance is IDictionary<string, object?> dictionary)
        {
            return dictionary.TryGetValue(name, out var found) ? ContextValueOutcome.Present(found) : ContextValueOutcome.Absent;
        }

        if (instance is IDictionary untyped)
        {
            return untyped.Contains(name) ? ContextValueOutcome.Present(untyped[name]) : ContextValueOutcome.Absent;
        }

        var property = instance.GetType().GetProperty(
            name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        return property is null || property.GetIndexParameters().Length > 0
            ? ContextValueOutcome.Absent
            : ContextValueOutcome.Present(property.GetValue(instance));
    }

    async ValueTask<ContextValueOutcome> ResolvePath(string path, IReadOnlyList<string> chain, CancellationToken cancellationToken)
    {
        var trimmed = path.StartsWith(Prefix, StringComparison.Ordinal) ? path[Prefix.Length..] : path;
        var segments = trimmed.Split('.');
        var head = await ResolveValue(segments[0], chain, cancellationToken);
        return Navigate(head, segments.Skip(1));
    }

    async ValueTask<ContextValueOutcome> ResolveValue(string name, IReadOnlyList<string> chain, CancellationToken cancellationToken)
    {
        if (chain.Contains(name))
        {
            throw new ContextValueCycleException([.. chain, name]);
        }

        if (name == ReservedContextNames.Audiences)
        {
            return await ResolveAudiences(cancellationToken);
        }

        if (ambient.TryGetValue(name, out var supplied))
        {
            return ContextValueOutcome.Present(supplied);
        }

        var declaration = catalog.Find(name, scope);
        if (declaration is null)
        {
            return ContextValueOutcome.Absent;
        }

        Lazy<Task<ContextValueOutcome>> lazy;
        lock (_lock)
        {
            if (!_resolved.TryGetValue(name, out lazy!))
            {
                var nested = new ChainedResolver(this, [.. chain, name]);
                lazy = new Lazy<Task<ContextValueOutcome>>(() => Provide(declaration, nested, cancellationToken));
                _resolved[name] = lazy;
            }
        }

        return await lazy.Value;
    }

    async ValueTask<ContextValueOutcome> ResolveAudiences(CancellationToken cancellationToken)
    {
        if (audiences is null)
        {
            return ContextValueOutcome.Absent;
        }

        Lazy<Task<ContextValueOutcome>> lazy;
        lock (_lock)
        {
            if (!_resolved.TryGetValue(ReservedContextNames.Audiences, out lazy!))
            {
                lazy = new Lazy<Task<ContextValueOutcome>>(async () =>
                    ContextValueOutcome.Present(await audiences(cancellationToken)));
                _resolved[ReservedContextNames.Audiences] = lazy;
            }
        }

        return await lazy.Value;
    }

    async Task<ContextValueOutcome> Provide(ContextValueDeclaration declaration, IContextValueResolver nested, CancellationToken cancellationToken)
    {
        ContextValueOutcome outcome;
        try
        {
            outcome = await declaration.Provider.Provide(nested, cancellationToken);
        }
        catch (Exception ex) when (ex is not (ContextValueCycleException or RequiredContextValueAbsentException or ContextValueProviderFailedException or OperationCanceledException))
        {
            throw new ContextValueProviderFailedException(declaration.Name, ex);
        }

        if (!outcome.IsPresent && declaration.Required)
        {
            throw new RequiredContextValueAbsentException(declaration.Name);
        }

        return outcome;
    }

    sealed class ChainedResolver(ContextValueResolver root, IReadOnlyList<string> chain) : IContextValueResolver
    {
        public ValueTask<ContextValueOutcome> Resolve(string path, CancellationToken cancellationToken = default) =>
            root.ResolvePath(path, chain, cancellationToken);
    }
}
