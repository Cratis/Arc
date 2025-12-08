// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Defines a builder for configuring Arc services or extensions.
/// </summary>
public interface IArcBuilder
{
    /// <summary>
    /// Gets the service collection to which services can be added.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the types system used by Arc.
    /// </summary>
    ITypes Types { get; }

    /// <summary>
    /// Gets or sets the Arc options configurator.
    /// </summary>
    Action<ArcOptions>? ConfigureOptions { get; set; }
}
