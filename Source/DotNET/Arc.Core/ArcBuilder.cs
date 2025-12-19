// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Represents a builder for configuring Arc services or extensions.
/// </summary>
/// <param name="Services">The service collection to which services can be added.</param>
/// <param name="Types">The types system used by Arc.</param>
public record ArcBuilder(IServiceCollection Services, ITypes Types) : IArcBuilder
{
    /// <summary>
    /// Gets or sets the Arc options configurator.
    /// </summary>
    public Action<ArcOptions>? ConfigureOptions { get; set; }
}