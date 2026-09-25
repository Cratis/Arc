// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries;

/// <summary>
/// Resolves a performer's protected member without letting an opaque performer hide method-level policies.
/// </summary>
internal static class QueryAuthorizationTarget
{
    /// <summary>
    /// Gets the method or type governing an actual query performer.
    /// </summary>
    /// <param name="performer">The performer.</param>
    /// <param name="declarations">Effective authorization declarations.</param>
    /// <returns>The member to authorize.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">An advanced method declaration cannot be resolved to one method.</exception>
    internal static MemberInfo For(IQueryPerformer performer, AuthorizationDeclarations declarations)
    {
        if (performer is IAuthorizationQueryTarget known)
        {
            return known.AuthorizationMethod;
        }

        var methods = performer.Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name == performer.Name.Value)
            .ToArray();
        if (methods.Length == 1)
        {
            return methods[0];
        }

        var protectedMethods = methods.Length > 1
            ? methods
            : performer.Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (protectedMethods.Any(method => declarations.For(method).RequiresAsynchronousEvaluation))
        {
            throw new InvalidAuthorizationConfiguration($"Query performer '{performer.FullyQualifiedName}' must expose an unambiguous authorization method to enforce its policy or schemes.");
        }

        return performer.Type;
    }
}
