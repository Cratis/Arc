// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.given;

/// <summary>
/// Composes the evaluator over <em>both</em> discovered <see cref="IAnonymousEvaluator"/> implementations, which is
/// what an ASP.NET Core host actually runs.
/// </summary>
/// <remarks>
/// Every other authorization spec exercises one evaluator alone, which is why the behavior pinned here was never
/// visible. See https://github.com/Cratis/Arc/issues/2714. These specs record what the code does today so that
/// changing it is a deliberate act; several of them describe behavior that is arguably wrong, and say so.
/// </remarks>
public class both_anonymous_evaluators : Specification
{
    protected ICurrentPrincipalAccessor _currentPrincipalAccessor;
    protected ClaimsPrincipal _user;

    void Establish()
    {
        _currentPrincipalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        _user = Substitute.For<ClaimsPrincipal>();
        _currentPrincipalAccessor.Current.Returns(_user);
    }

    /// <summary>
    /// Builds the evaluator with the anonymous evaluators in a specific order, because the first one to answer wins.
    /// </summary>
    /// <param name="anonymousEvaluators">The <see cref="IAnonymousEvaluator"/> implementations, in discovery order.</param>
    /// <returns>The composed <see cref="AuthorizationEvaluator"/>.</returns>
    protected AuthorizationEvaluator EvaluatorWith(params IAnonymousEvaluator[] anonymousEvaluators)
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => anonymousEvaluators.AsEnumerable().GetEnumerator());

        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new List<IAuthorizationAttributeEvaluator>
        {
            new AuthorizationAttributeEvaluator(),
            new AspNetAuthorizationAttributeEvaluator()
        }.GetEnumerator());

        return new AuthorizationEvaluator(_currentPrincipalAccessor, anonymous, attributes);
    }

    /// <summary>
    /// Carries Arc's own contradictory markings.
    /// </summary>
    [AllowAnonymous]
    [Authorize]
    public class WithArcAttributes;

    /// <summary>
    /// Carries only ASP.NET Core's authorization marking, fully qualified so it cannot be confused with Arc's
    /// same-named attribute.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class WithAspNetAuthorizeOnly;
}
