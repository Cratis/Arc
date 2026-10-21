// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Arc.Connections;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Represents the configuration options for Entity Framework Core.
/// </summary>
public class EntityFrameworkCoreOptions
{
    /// <summary>
    /// Gets or sets the connection string for the database.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically discover and register DbContext types
    /// that inherit from <see cref="BaseDbContext"/> or <see cref="ReadOnlyDbContext"/>.
    /// Defaults to true.
    /// </summary>
    public bool AutoDiscoverDbContexts { get; set; } = true;

    /// <summary>
    /// Gets the additional <see cref="JsonConverter"/> instances to include when serializing and
    /// deserializing <c>[Json]</c>-annotated entity properties. These converters are appended after
    /// the built-in Arc defaults (ConceptAs, Enum, DateOnly, TimeOnly, Type, Uri, …).
    /// </summary>
    public IList<JsonConverter> JsonConverters { get; } = [];

    /// <summary>
    /// Gets or sets how connections are resolved per tenant, overriding the application-wide
    /// <see cref="ArcOptions.ConnectionResolution"/> for Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// When neither is configured, <see cref="ConnectionString"/> is used as it is. Otherwise every connection is resolved
    /// for the current tenant when it opens, and the result is applied over the configured connection string.
    /// </remarks>
    public ConnectionResolutionOptions ConnectionResolution { get; set; } = new();
}
