// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Resolves the discovery marker on a query's owning type or exact method.
/// </summary>
internal static class QueryDiscoveryExtensions
{
    /// <summary>
    /// Determines whether the owning type or the query method excludes the performer from discovery.
    /// </summary>
    /// <param name="performer">The query performer.</param>
    /// <returns>True when the performer is hidden from discovery.</returns>
    internal static bool IsExcludedFromDiscovery(this IQueryPerformer performer)
    {
        if (performer.Type is not { } type)
        {
            return false;
        }

        if (type.IsDefined(typeof(ExcludeFromDiscoveryAttribute), true) ||
            performer.ReadModelType?.IsDefined(typeof(ExcludeFromDiscoveryAttribute), true) == true)
        {
            return true;
        }

        if (performer is IAuthorizationQueryTarget known)
        {
            return known.AuthorizationMethod.IsDefined(typeof(ExcludeFromDiscoveryAttribute), true);
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name == performer.Name.Value).ToArray();
        return methods.Length == 1 && methods[0].IsDefined(typeof(ExcludeFromDiscoveryAttribute), true);
    }
}
