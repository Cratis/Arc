// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves every request to one configured tenant ID.
/// </summary>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
/// <remarks>
/// The tenant ID comes from <see cref="TenancyOptions.FixedTenantId"/> and is returned regardless of the request or the
/// hosting environment. It is the resolver for single tenant deployments, where the tenant is a deployment level
/// constant rather than something a request carries.
/// </remarks>
[IgnoreConvention]
public class FixedTenantIdResolver(IOptions<ArcOptions> options) : ITenantIdResolver
{
    /// <inheritdoc/>
    public string Resolve() => options.Value.Tenancy.FixedTenantId;
}
