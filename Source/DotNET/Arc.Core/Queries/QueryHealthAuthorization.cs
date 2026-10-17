// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Applies the configured query health access requirements at pipeline authorization boundaries.
/// </summary>
/// <param name="options">The configured Arc options.</param>
/// <param name="environment">The host environment.</param>
public class QueryHealthAuthorization(IOptions<ArcOptions> options, IHostEnvironment environment) : IAnonymousEvaluator, IAuthorizationAttributeEvaluator
{
    /// <inheritdoc/>
    public bool? IsAnonymousAllowed(Type type) => AnonymousAccess(type);

    /// <inheritdoc/>
    public bool? IsAnonymousAllowed(MethodInfo method) => AnonymousAccess(method);

    /// <inheritdoc/>
    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => AuthorizationInfo(type);

    /// <inheritdoc/>
    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => AuthorizationInfo(method);

    bool? AnonymousAccess(MemberInfo member) => QueryHealthEndpointAccess.IsQueryHealth(member)
        ? !options.Value.QueryHealth.RequiresAuthentication(environment.IsDevelopment())
        : null;

    (bool HasAuthorize, string? Roles)? AuthorizationInfo(MemberInfo member) => AnonymousAccess(member) == false
        ? (true, options.Value.QueryHealth.Roles)
        : null;
}
