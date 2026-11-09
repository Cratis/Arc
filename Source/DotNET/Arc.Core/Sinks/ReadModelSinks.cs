// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IReadModelSinks"/>.
/// </summary>
/// <remarks>
/// A <see cref="ReadModelSinkAttribute"/> on the type wins; otherwise the registered <see cref="IReadModelSinkNameProvider"/>
/// instances are asked in order. A blank name means the default sink. The answer is kept per type.
/// </remarks>
/// <param name="providers">The registered <see cref="IReadModelSinkNameProvider"/> instances.</param>
[Singleton]
public class ReadModelSinks(IEnumerable<IReadModelSinkNameProvider> providers) : IReadModelSinks
{
    readonly IReadModelSinkNameProvider[] _providers = [.. providers];
    readonly ConcurrentDictionary<Type, string?> _sinkNames = new();

    /// <inheritdoc/>
    public string? GetSinkNameFor(Type readModelType) =>
        _sinkNames.GetOrAdd(readModelType, static (type, providers) => Resolve(type, providers), _providers);

    static string? Resolve(Type readModelType, IReadModelSinkNameProvider[] providers)
    {
        var declared = readModelType.GetCustomAttribute<ReadModelSinkAttribute>()?.Name;
        if (!string.IsNullOrWhiteSpace(declared))
        {
            return declared.Trim();
        }

        foreach (var provider in providers)
        {
            var name = provider.GetSinkNameFor(readModelType);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }
        }

        return null;
    }
}
