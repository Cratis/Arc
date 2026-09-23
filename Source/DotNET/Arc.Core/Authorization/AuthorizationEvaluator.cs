// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Represents an implementation of <see cref="IAuthorizationEvaluator"/> that decides authorization from the
/// authorization attributes declared on a type or method.
/// </summary>
/// <param name="currentPrincipalAccessor">The <see cref="ICurrentPrincipalAccessor"/> to access the principal currently executing.</param>
/// <param name="anonymousEvaluators">The collection of <see cref="IAnonymousEvaluator"/> instances.</param>
/// <param name="authorizationAttributeEvaluators">The collection of <see cref="IAuthorizationAttributeEvaluator"/> instances.</param>
/// <remarks>
/// <para>
/// Every evaluator is consulted, so the outcome never depends on the order evaluators are discovered in. Each
/// attribute family - Arc's own, and ASP.NET Core's when Arc is hosted there - contributes through its own evaluator.
/// </para>
/// <para>
/// A member that one evaluator reports as anonymous and another as restricted contradicts itself and is rejected
/// with <see cref="AmbiguousAuthorizationLevel"/> rather than resolved toward either reading. Every authorization
/// requirement found must be satisfied, so stacked attributes require all of them.
/// </para>
/// <para>
/// A method's own declaration replaces its type's: a restricted read model can open one query, and an open read
/// model can restrict one.
/// </para>
/// </remarks>
public class AuthorizationEvaluator(
    ICurrentPrincipalAccessor currentPrincipalAccessor,
    IInstancesOf<IAnonymousEvaluator> anonymousEvaluators,
    IInstancesOf<IAuthorizationAttributeEvaluator> authorizationAttributeEvaluators) : IAuthorizationEvaluator
{
    /// <inheritdoc/>
    /// <exception cref="AmbiguousAuthorizationLevel">Thrown when the type is declared both anonymous and restricted.</exception>
    public bool IsAuthorized(Type type)
    {
        if (IsAnonymousAllowed(type, evaluator => evaluator.IsAnonymousAllowed(type)) == true)
        {
            return true;
        }

        return Satisfies(authorizationAttributeEvaluators.SelectMany(evaluator => evaluator.GetAuthorizationRequirements(type)));
    }

    /// <inheritdoc/>
    /// <exception cref="AmbiguousAuthorizationLevel">Thrown when the method or its type is declared both anonymous and restricted.</exception>
    public bool IsAuthorized(MethodInfo method)
    {
        if (IsAnonymousAllowed(method, evaluator => evaluator.IsAnonymousAllowed(method)) == true)
        {
            return true;
        }

        var requirements = authorizationAttributeEvaluators
            .SelectMany(evaluator => evaluator.GetAuthorizationRequirements(method))
            .ToList();

        if (requirements.Count > 0)
        {
            return Satisfies(requirements);
        }

        var declaringType = method.DeclaringType;
        return declaringType is null || IsAuthorized(declaringType);
    }

    /// <summary>
    /// Asks every anonymous evaluator about a member and combines their answers.
    /// </summary>
    /// <param name="member">The member being evaluated, used to describe a contradiction.</param>
    /// <param name="ask">Asks one evaluator about the member.</param>
    /// <returns>True when the member is anonymous, false when it is restricted, or null when no evaluator declares either.</returns>
    /// <exception cref="AmbiguousAuthorizationLevel">Thrown when one evaluator reports the member anonymous and another restricted.</exception>
    bool? IsAnonymousAllowed(MemberInfo member, Func<IAnonymousEvaluator, bool?> ask)
    {
        var answers = anonymousEvaluators.Select(ask).Where(answer => answer.HasValue).Select(answer => answer!.Value).ToList();

        if (answers.Contains(true) && answers.Contains(false))
        {
            throw new AmbiguousAuthorizationLevel(member);
        }

        return answers.Count == 0 ? null : answers[0];
    }

    bool Satisfies(IEnumerable<AuthorizationRequirement> requirements)
    {
        var all = requirements.ToList();
        if (all.Count == 0)
        {
            return true;
        }

        var user = currentPrincipalAccessor.Current;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return all.TrueForAll(requirement => requirement.AnyOfRoles.Count == 0 || requirement.AnyOfRoles.Any(user.IsInRole));
    }
}
