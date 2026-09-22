// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_a_declaration_marked_with_aspnet_core_attributes;

/// <summary>
/// <see cref="AspNetAnonymousEvaluator"/> and <see cref="AspNetAuthorizationAttributeEvaluator"/> are named for
/// ASP.NET Core's attributes and documented as reading them, but neither file imports
/// <c>Microsoft.AspNetCore.Authorization</c>, and both are declared in the <c>Cratis.Arc.Authorization</c> namespace
/// - so the unqualified <c>AuthorizeAttribute</c> and <c>AllowAnonymousAttribute</c> in them bind to Arc's own
/// same-named types. The result is that a declaration marked only with ASP.NET Core's <c>[Authorize]</c> is not
/// recognized by any evaluator, and the caller is admitted.
/// </summary>
/// <remarks>
/// Pinned, not endorsed. Their existing specs mark their fixtures with the unqualified attribute from inside the
/// same namespace, so those pass without ever exercising an ASP.NET Core attribute.
/// </remarks>
public class with_authorize_only : given.both_anonymous_evaluators
{
    bool _isAuthorized;

    void Because() => _isAuthorized =
        EvaluatorWith(new AnonymousEvaluator(), new AspNetAnonymousEvaluator()).IsAuthorized(typeof(WithAspNetAuthorizeOnly));

    [Fact] void should_not_enforce_the_aspnet_core_marking() => _isAuthorized.ShouldBeTrue();
}
