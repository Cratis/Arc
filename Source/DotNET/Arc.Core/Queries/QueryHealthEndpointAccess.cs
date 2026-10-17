// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Grants only the query health feed its configured anonymous endpoint access.
/// </summary>
internal static class QueryHealthEndpointAccess
{
    /// <summary>
    /// Checks whether the health query explicitly permits anonymous endpoint access.
    /// </summary>
    /// <param name="performer">The query performer.</param>
    /// <param name="services">The host services used to resolve access options and the environment.</param>
    /// <returns>Whether host fallback authentication may be bypassed for this health query.</returns>
    internal static bool ForQuery(IQueryPerformer performer, IServiceProvider services)
    {
        var isQueryHealth = performer is IAuthorizationQueryTarget target
            ? IsQueryHealth(target.AuthorizationMethod)
            : performer.Type == typeof(QueryHealth) && performer.Name.Value == nameof(QueryHealth.ObserveHealth);
        if (!isQueryHealth)
        {
            return false;
        }

        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetService<IOptions<ArcOptions>>();
        if (options is null)
        {
            return false;
        }

        var isDevelopment = scope.ServiceProvider.GetService<IHostEnvironment>()?.IsDevelopment() ?? RuntimeEnvironment.IsDevelopment;
        return !options.Value.QueryHealth.RequiresAuthentication(isDevelopment);
    }

    /// <summary>
    /// Matches only the built-in health read model or its observable query method.
    /// </summary>
    /// <param name="member">The protected member.</param>
    /// <returns>Whether the member belongs to the health feed.</returns>
    internal static bool IsQueryHealth(MemberInfo member) => member == typeof(QueryHealth) ||
        (member is MethodInfo method && method.DeclaringType == typeof(QueryHealth) && method.Name == nameof(QueryHealth.ObserveHealth));
}
