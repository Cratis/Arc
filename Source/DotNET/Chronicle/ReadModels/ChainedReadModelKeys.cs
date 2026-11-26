// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Registers only the read-model properties referenced by chained lookups; for generated code only.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ChainedReadModelKeys
{
    static readonly ConcurrentDictionary<(Type Model, string Property), CommandReadModelKeyProperty> _properties = new();

    /// <summary>
    /// Registers statically typed readers and property types for a model; for generated code only.
    /// </summary>
    /// <param name="model">The exact source read-model type.</param>
    /// <param name="properties">The referenced public readable non-indexed instance properties.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterModel(Type model, params CommandReadModelKeyProperty[] properties)
    {
        foreach (var property in properties) _properties.TryAdd((model, property.Name), property);
    }

    /// <summary>
    /// Finds a generated relationship property without reflection fallback.
    /// </summary>
    /// <param name="model">The exact source model type.</param>
    /// <param name="property">The referenced property name.</param>
    /// <returns>The registered descriptor, or null when unsupported.</returns>
    internal static CommandReadModelKeyProperty? PropertyFor(Type model, string property) => _properties.GetValueOrDefault((model, property));
}
