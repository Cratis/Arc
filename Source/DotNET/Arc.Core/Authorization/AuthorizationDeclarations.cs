// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Resolves explicit authorization declarations before consulting baseline fallback evaluators.
/// </summary>
/// <param name="anonymousEvaluators">The anonymous declarations.</param>
/// <param name="attributeEvaluators">The restricted declarations.</param>
public class AuthorizationDeclarations(
    IInstancesOf<IAnonymousEvaluator> anonymousEvaluators,
    IInstancesOf<IAuthorizationAttributeEvaluator> attributeEvaluators)
{
    readonly IEnumerable<IFallbackAuthorizationEvaluator> _fallbackEvaluators = [];

    /// <summary>
    /// Resolves explicit declarations and discovers baseline requirements by convention.
    /// </summary>
    /// <param name="anonymousEvaluators">The anonymous declarations.</param>
    /// <param name="attributeEvaluators">The restricted declarations.</param>
    /// <param name="fallbackEvaluators">Baseline requirements used only when no explicit declaration applies.</param>
    public AuthorizationDeclarations(
        IInstancesOf<IAnonymousEvaluator> anonymousEvaluators,
        IInstancesOf<IAuthorizationAttributeEvaluator> attributeEvaluators,
        IInstancesOf<IFallbackAuthorizationEvaluator> fallbackEvaluators) : this(anonymousEvaluators, attributeEvaluators) =>
        _fallbackEvaluators = fallbackEvaluators;

    /// <summary>
    /// Resolves the effective declaration on a type.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <returns>The declaration.</returns>
    public AuthorizationDeclaration For(Type type)
    {
        var declaration = Resolve(type, evaluator => evaluator.IsAnonymousAllowed(type), evaluator => evaluator.GetAuthorizationRequirements(type));
        return declaration.IsExplicit ? declaration : Baseline(_fallbackEvaluators.SelectMany(evaluator => evaluator.GetAuthorizationRequirements(type)));
    }

    /// <summary>
    /// Resolves the effective declaration on a method, falling back to its declaring type.
    /// </summary>
    /// <param name="method">The declared method.</param>
    /// <returns>The declaration.</returns>
    public AuthorizationDeclaration For(MethodInfo method)
    {
        var declaration = Resolve(method, evaluator => evaluator.IsAnonymousAllowed(method), evaluator => evaluator.GetAuthorizationRequirements(method));
        if (declaration.IsExplicit)
        {
            return declaration;
        }

        if (method.DeclaringType is { } type)
        {
            declaration = Resolve(type, evaluator => evaluator.IsAnonymousAllowed(type), evaluator => evaluator.GetAuthorizationRequirements(type));
            if (declaration.IsExplicit)
            {
                return declaration;
            }
        }

        return Baseline(_fallbackEvaluators.SelectMany(evaluator => evaluator.GetAuthorizationRequirements(method)
            .Concat(method.DeclaringType is { } declaringType ? evaluator.GetAuthorizationRequirements(declaringType) : [])));
    }

    static AuthorizationDeclaration Baseline(IEnumerable<AuthorizationRequirement> requirements) =>
        new(false, false, requirements.ToArray());

    AuthorizationDeclaration Resolve(
        MemberInfo member,
        Func<IAnonymousEvaluator, bool?> ask,
        Func<IAuthorizationAttributeEvaluator, IEnumerable<AuthorizationRequirement>> requirementsOf)
    {
        var answers = anonymousEvaluators.Select(evaluator => (Evaluator: evaluator, Answer: ask(evaluator)))
            .Where(result => result.Answer.HasValue).ToArray();
        var anonymous = answers.FirstOrDefault(result => result.Answer == true).Evaluator;
        var restricted = answers.FirstOrDefault(result => result.Answer == false).Evaluator;
        if (anonymous is not null && restricted is not null)
        {
            throw new AmbiguousAuthorizationLevel(member, anonymous.GetType(), restricted.GetType());
        }

        var requirements = attributeEvaluators.Select(evaluator => (Evaluator: evaluator, Requirements: requirementsOf(evaluator).ToArray())).ToArray();
        if (anonymous is not null && requirements.FirstOrDefault(result => result.Requirements.Length > 0).Evaluator is { } conflicting)
        {
            throw new AmbiguousAuthorizationLevel(member, anonymous.GetType(), conflicting.GetType());
        }

        return new AuthorizationDeclaration(
            anonymous is not null,
            anonymous is not null || requirements.Any(result => result.Requirements.Length > 0),
            requirements.SelectMany(result => result.Requirements).ToArray());
    }
}
