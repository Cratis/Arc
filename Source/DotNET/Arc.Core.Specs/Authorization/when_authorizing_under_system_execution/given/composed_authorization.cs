// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authorization.when_authorizing_under_system_execution.given;

/// <summary>
/// Composes the real <see cref="SystemExecution"/>, <see cref="CurrentPrincipalAccessor"/> and
/// <see cref="AuthorizationEvaluator"/> — with no HTTP request present — so a role-gated type can be
/// authorized end-to-end through a server-side execution scope.
/// </summary>
public class composed_authorization : Specification
{
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected CurrentPrincipalAccessor _currentPrincipalAccessor;
    protected SystemExecution _systemExecution;
    protected AuthorizationEvaluator _authorizationEvaluator;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _httpRequestContextAccessor.Current.Returns((IHttpRequestContext?)null);

        _currentPrincipalAccessor = new CurrentPrincipalAccessor(_httpRequestContextAccessor);
        _systemExecution = new SystemExecution(_currentPrincipalAccessor);

        var anonymousEvaluators = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymousEvaluators.GetEnumerator().Returns(_ => new List<IAnonymousEvaluator> { new AnonymousEvaluator() }.GetEnumerator());

        var authorizationAttributeEvaluators = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        authorizationAttributeEvaluators.GetEnumerator().Returns(_ => new List<IAuthorizationAttributeEvaluator> { new AuthorizationAttributeEvaluator() }.GetEnumerator());

        _authorizationEvaluator = new AuthorizationEvaluator(_currentPrincipalAccessor, anonymousEvaluators, authorizationAttributeEvaluators);
    }

    [Authorize(Roles = "Admin")]
    public class RoleGatedType;
}
