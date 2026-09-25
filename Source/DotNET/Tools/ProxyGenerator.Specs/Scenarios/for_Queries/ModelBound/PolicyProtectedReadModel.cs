// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using ArcPrincipalAccessor = Cratis.Arc.Authorization.ICurrentPrincipalAccessor;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// A query whose ASP.NET Core policy contributes an authentication scheme.
/// </summary>
/// <param name="Value">The result value.</param>
[ReadModel]
[Authorize(Policy = "ActiveSubscription")]
public record PolicyProtectedReadModel(string Value)
{
    static int _performed;
    static string? _lastHttpCaller;
    static string? _lastBoundTenant;
    static Guid _lastBoundServiceId;
    static string? _lastHttpScopeTenant;

    /// <summary>
    /// Gets the number of executions.
    /// </summary>
    public static int Performed => Volatile.Read(ref _performed);

    /// <summary>
    /// Gets the ASP.NET Core principal observed while performing the query.
    /// </summary>
    public static string? LastHttpCaller => Volatile.Read(ref _lastHttpCaller);

    /// <summary>
    /// Gets the tenant captured by the query's scoped dependency.
    /// </summary>
    public static string? LastBoundTenant => Volatile.Read(ref _lastBoundTenant);

    /// <summary>Gets the scoped instance injected into the query method.</summary>
    public static Guid LastBoundServiceId => _lastBoundServiceId;

    /// <summary>
    /// Gets the tenant resolved through the selected HTTP execution scope.
    /// </summary>
    public static string? LastHttpScopeTenant => Volatile.Read(ref _lastHttpScopeTenant);

    /// <summary>
    /// Returns the selected identity's data.
    /// </summary>
    /// <param name="principal">The Arc principal.</param>
    /// <param name="httpContextAccessor">The ASP.NET Core request principal.</param>
    /// <param name="bound">A scoped collaborator capturing the tenant when constructed.</param>
    /// <returns>A protected read model.</returns>
    public static PolicyProtectedReadModel All(ArcPrincipalAccessor principal, IHttpContextAccessor httpContextAccessor, TenantBoundService bound)
    {
        var httpContext = httpContextAccessor.HttpContext!;
        Volatile.Write(ref _lastHttpCaller, httpContext.User.Identity?.Name);
        Volatile.Write(ref _lastBoundTenant, bound.Tenant.Value);
        _lastBoundServiceId = bound.Id;
        Volatile.Write(ref _lastHttpScopeTenant, httpContext.RequestServices.GetRequiredService<TenantBoundService>().Tenant.Value);
        Interlocked.Increment(ref _performed);
        return new(principal.Current?.Identity?.Name ?? "Missing");
    }
}
