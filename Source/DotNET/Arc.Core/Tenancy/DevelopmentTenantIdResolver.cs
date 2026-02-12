// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves to a fixed tenant ID for development purposes.
/// </summary>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
public class DevelopmentTenantIdResolver(IOptions<ArcOptions> options) : ITenantIdResolver
{
    /// <inheritdoc/>
    public string Resolve() => options.Value.Tenancy.DevelopmentTenantId;
}
