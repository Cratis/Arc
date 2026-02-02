// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Defines a builder for configuring Entity Framework Core.
/// </summary>
public interface IEntityFrameworkCoreBuilder
{
    /// <summary>
    /// Gets the service collection to which services can be added.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the types system used for discovery.
    /// </summary>
    ITypes Types { get; }

    /// <summary>
    /// Gets or sets the <see cref="EntityFrameworkCoreOptions"/>.
    /// </summary>
    EntityFrameworkCoreOptions Options { get; set; }
}
