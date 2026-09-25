// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Resolves the effective authorization declaration, including both attribute families.
/// </summary>
/// <param name="anonymousEvaluators">The anonymous declarations.</param>
/// <param name="attributeEvaluators">The restricted declarations.</param>
public class AuthorizationDeclarations(
    IInstancesOf<IAnonymousEvaluator> anonymousEvaluators,
    IInstancesOf<IAuthorizationAttributeEvaluator> attributeEvaluators)
{
    /// <summary>
    /// Resolves the effective declaration on a type.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <returns>The declaration.</returns>
    public AuthorizationDeclaration For(Type type) => Resolve(type, evaluator => evaluator.IsAnonymousAllowed(type), evaluator => evaluator.GetAuthorizationRequirements(type));

    /// <summary>
    /// Resolves the effective declaration on a method, falling back to its declaring type.
    /// </summary>
    /// <param name="method">The declared method.</param>
    /// <returns>The declaration.</returns>
    public AuthorizationDeclaration For(MethodInfo method)
    {
        var declaration = Resolve(method, evaluator => evaluator.IsAnonymousAllowed(method), evaluator => evaluator.GetAuthorizationRequirements(method));
        return declaration.IsExplicit ? declaration : method.DeclaringType is { } type ? For(type) : declaration;
    }

    AuthorizationDeclaration Resolve(
        MemberInfo member,
        Func<IAnonymousEvaluator, bool?> ask,
        Func<IAuthorizationAttributeEvaluator, IEnumerable<AuthorizationRequirement>> requirementsOf)
    {
        var answers = anonymousEvaluators.Select(ask).Where(answer => answer.HasValue).Select(answer => answer!.Value).ToArray();
        if (answers.Contains(true) && answers.Contains(false))
        {
            throw new AmbiguousAuthorizationLevel(member);
        }

        var requirements = attributeEvaluators.SelectMany(requirementsOf).ToArray();
        if (answers.Contains(true) && requirements.Length > 0)
        {
            throw new AmbiguousAuthorizationLevel(member);
        }

        return new AuthorizationDeclaration(answers.Contains(true), answers.Contains(true) || requirements.Length > 0, requirements);
    }
}
