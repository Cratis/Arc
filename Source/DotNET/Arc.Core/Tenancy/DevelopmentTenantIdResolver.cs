// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves to a fixed tenant ID for development purposes.
/// </summary>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
/// <remarks>
/// This is <see cref="FixedTenantIdResolver"/> under its original name and resolves the same configured tenant ID.
/// The resolver never consulted the hosting environment, so prefer <see cref="FixedTenantIdResolver"/> and
/// <see cref="TenantResolverType.Fixed"/> when the fixed tenant is a deployment level constant rather than a
/// development convenience.
/// </remarks>
[IgnoreConvention]
public class DevelopmentTenantIdResolver(IOptions<ArcOptions> options) : FixedTenantIdResolver(options);
