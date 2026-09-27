// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json.Serialization;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Carries admission-only data back across the asynchronous query pipeline boundary without serializing it.
/// </summary>
public partial class QueryResult
{
    /// <summary>
    /// Gets the authorized principal explicitly returned to a subscription caller without retaining its request scope.
    /// </summary>
    [JsonIgnore]
    internal ClaimsPrincipal? AuthorizedPrincipal { get; set; }

    /// <summary>
    /// Gets the selected tenant independently of the caller's restored AsyncLocal cache.
    /// </summary>
    [JsonIgnore]
    internal TenantId? AuthorizedTenant { get; set; }

    /// <summary>
    /// Gets the validated and coerced arguments returned to a subscription caller.
    /// </summary>
    [JsonIgnore]
    internal QueryArguments? AuthorizedArguments { get; set; }

    /// <summary>
    /// Gets the admitted query context for the direct transport after the pipeline's asynchronous frame returns.
    /// </summary>
    [JsonIgnore]
    internal QueryContext? AuthorizedQueryContext { get; set; }

    /// <summary>
    /// Gets a fresh identity-bound query scope transferred to the transport until response or subscription completion.
    /// </summary>
    [JsonIgnore]

    // Cratis.Arc.Testing accesses this through InternalsVisibleTo and releases the scope after snapshot materialization.
    internal IServiceScope? OwnedScope { get; set; }
}
