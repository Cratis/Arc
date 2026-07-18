// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Execution;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Cratis.Execution;

namespace Cratis.Arc;

/// <summary>
/// Represents the options for Arc.
/// </summary>
public class ArcOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArcOptions"/> class.
    /// </summary>
    public ArcOptions()
    {
        JsonSerializerOptions = new JsonSerializerOptions().ConfigureArcDefaults(Internals.DerivedTypesOrDefault);
    }

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> configured for Arc.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; }

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
    /// Gets or sets the options for observable queries.
    /// </summary>
    public QueryOptions Query { get; set; } = new();

    /// <summary>
    /// Gets or sets the hosting options for Arc, only used by Arc.Core.
    /// </summary>
    public HostingOptions Hosting { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether exception detail (messages and stack traces) is exposed
    /// to clients in serialized <see cref="Commands.CommandResult"/> and <see cref="Queries.QueryResult"/> responses.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/> only in the Development environment and <see langword="false"/> otherwise.
    /// When <see langword="false"/>, exception messages and stack traces are redacted from responses (the full detail
    /// is still logged server-side and the correlation identifier is retained) to avoid leaking internal information.
    /// </remarks>
    public bool ExposeExceptionDetails { get; set; } = RuntimeEnvironment.IsDevelopment;
}
