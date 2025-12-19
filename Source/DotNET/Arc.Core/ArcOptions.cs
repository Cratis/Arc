// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Execution;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc;

/// <summary>
/// Represents the options for Arc.
/// </summary>
public class ArcOptions
{
    /// <summary>
    /// Gets or sets the options for the correlation ID.
    /// </summary>
    public CorrelationIdOptions CorrelationId { get; set; } = new();

    /// <summary>
    /// Gets or sets the options for the tenancy.
    /// </summary>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <summary>
    /// Gets or sets what type of identity details provider to use. If none is specified it will use type discovery to try to find one.
    /// </summary>
    public Type? IdentityDetailsProvider { get; set; }

    /// <summary>
    /// Gets or sets the options for generated API endpoints (commands and queries).
    /// </summary>
    public ApiEndpointOptions GeneratedApis { get; set; } = new();

    /// <summary>
    /// Gets or sets the hosting options for Arc, only used by Arc.Core.
    /// </summary>
    public HostingOptions Hosting { get; set; } = new();
}
