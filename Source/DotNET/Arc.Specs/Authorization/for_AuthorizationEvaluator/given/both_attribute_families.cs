// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.given;

/// <summary>
/// Composes the evaluator over both attribute families, the way an ASP.NET Core host runs it: Arc's own attributes
/// through <see cref="AnonymousEvaluator"/> and <see cref="AuthorizationAttributeEvaluator"/>, and ASP.NET Core's
/// through <see cref="AspNetAnonymousEvaluator"/> and <see cref="AspNetAuthorizationAttributeEvaluator"/>.
/// </summary>
/// <remarks>
/// Every behavior is checked with the evaluators in both orders, because discovery order is not a contract and the
/// outcome must not depend on it.
/// </remarks>
public class both_attribute_families : Specification
{
    protected ICurrentPrincipalAccessor _currentPrincipalAccessor;

    void Establish()
    {
        _currentPrincipalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        _currentPrincipalAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    protected void SignedInWithRoles(params string[] roles) =>
        _currentPrincipalAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            roles.Select(role => new Claim(ClaimTypes.Role, role)).Prepend(new Claim(ClaimTypes.Name, "someone")),
            "test")));

    /// <summary>
    /// Builds the evaluator with Arc's evaluators first.
    /// </summary>
    /// <returns>The composed <see cref="AuthorizationEvaluator"/>.</returns>
    protected AuthorizationEvaluator ArcFirst() => Compose(
        [new AnonymousEvaluator(), new AspNetAnonymousEvaluator()],
        [new AuthorizationAttributeEvaluator(), new AspNetAuthorizationAttributeEvaluator()]);

    /// <summary>
    /// Builds the evaluator with ASP.NET Core's evaluators first.
    /// </summary>
    /// <returns>The composed <see cref="AuthorizationEvaluator"/>.</returns>
    protected AuthorizationEvaluator AspNetFirst() => Compose(
        [new AspNetAnonymousEvaluator(), new AnonymousEvaluator()],
        [new AspNetAuthorizationAttributeEvaluator(), new AuthorizationAttributeEvaluator()]);

    /// <summary>
    /// Creates effective metadata resolution with Arc attributes first.
    /// </summary>
    /// <returns>The declaration resolver.</returns>
    protected AuthorizationDeclarations ArcDeclarationsFirst() => BuildDeclarations(
        [new AnonymousEvaluator(), new AspNetAnonymousEvaluator()],
        [new AuthorizationAttributeEvaluator(), new AspNetAuthorizationAttributeEvaluator()]);

    /// <summary>
    /// Creates effective metadata resolution with ASP.NET Core attributes first.
    /// </summary>
    /// <returns>The declaration resolver.</returns>
    protected AuthorizationDeclarations AspNetDeclarationsFirst() => BuildDeclarations(
        [new AspNetAnonymousEvaluator(), new AnonymousEvaluator()],
        [new AspNetAuthorizationAttributeEvaluator(), new AuthorizationAttributeEvaluator()]);

    AuthorizationEvaluator Compose(IAnonymousEvaluator[] anonymous, IAuthorizationAttributeEvaluator[] attributes)
    {
        var (anonymousEvaluators, attributeEvaluators) = CreateEvaluators(anonymous, attributes);
        return new AuthorizationEvaluator(_currentPrincipalAccessor, anonymousEvaluators, attributeEvaluators);
    }

    AuthorizationDeclarations BuildDeclarations(IAnonymousEvaluator[] anonymous, IAuthorizationAttributeEvaluator[] attributes)
    {
        var (anonymousEvaluators, attributeEvaluators) = CreateEvaluators(anonymous, attributes);
        return new AuthorizationDeclarations(anonymousEvaluators, attributeEvaluators);
    }

    static (IInstancesOf<IAnonymousEvaluator>, IInstancesOf<IAuthorizationAttributeEvaluator>) CreateEvaluators(
        IAnonymousEvaluator[] anonymous,
        IAuthorizationAttributeEvaluator[] attributes)
    {
        var anonymousEvaluators = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymousEvaluators.GetEnumerator().Returns(_ => anonymous.AsEnumerable().GetEnumerator());
        var attributeEvaluators = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributeEvaluators.GetEnumerator().Returns(_ => attributes.AsEnumerable().GetEnumerator());
        return (anonymousEvaluators, attributeEvaluators);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProtectedWithAspNet;

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class AdminOnlyWithAspNet;

    [Authorize(Roles = "Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Auditor")]
    public class RequiringBothRoles;

    [AllowAnonymous]
    [Authorize]
    public class ContradictingWithArc;

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ContradictingWithAspNet;

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Authorize]
    public class ContradictingAcrossFamilies;

    [Authorize]
    public static class ProtectedReadModel
    {
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public static void Public()
        {
        }

        public static void Protected()
        {
        }
    }
}
