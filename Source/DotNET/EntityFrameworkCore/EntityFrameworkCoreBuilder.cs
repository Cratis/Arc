// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Represents an implementation of <see cref="IEntityFrameworkCoreBuilder"/>.
/// </summary>
/// <param name="services">The <see cref="IServiceCollection"/>.</param>
/// <param name="types">The <see cref="ITypes"/> for type discovery.</param>
public class EntityFrameworkCoreBuilder(IServiceCollection services, ITypes types) : IEntityFrameworkCoreBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; } = services;

    /// <inheritdoc/>
    public ITypes Types { get; } = types;

    /// <inheritdoc/>
    public EntityFrameworkCoreOptions Options { get; set; } = new();
}
