// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.given;

public class an_authorization_helper : Specification
{
    protected ICurrentPrincipalAccessor _currentPrincipalAccessor;
    protected AuthorizationEvaluator _authorizationHelper;
    protected ClaimsPrincipal _user;
    protected IInstancesOf<IAnonymousEvaluator> _anonymousEvaluators;
    protected IInstancesOf<IAuthorizationAttributeEvaluator> _authorizationAttributeEvaluators;

    void Establish()
    {
        _currentPrincipalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        _user = Substitute.For<ClaimsPrincipal>();

        _currentPrincipalAccessor.Current.Returns(_user);

        _anonymousEvaluators = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        _anonymousEvaluators.GetEnumerator().Returns(_ => new List<IAnonymousEvaluator> { new AnonymousEvaluator() }.GetEnumerator());

        _authorizationAttributeEvaluators = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        _authorizationAttributeEvaluators.GetEnumerator().Returns(_ => new List<IAuthorizationAttributeEvaluator> { new AuthorizationAttributeEvaluator() }.GetEnumerator());

        _authorizationHelper = new AuthorizationEvaluator(_currentPrincipalAccessor, _anonymousEvaluators, _authorizationAttributeEvaluators);
    }

    protected void SetupAuthenticatedUser(params string[] roles)
    {
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        _user.Identity.Returns(identity);

        foreach (var role in roles)
        {
            _user.IsInRole(role).Returns(true);
        }
    }

    protected void SetupUnauthenticatedUser()
    {
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(false);
        _user.Identity.Returns(identity);
    }

    protected void SetupNoHttpRequestContext() => _currentPrincipalAccessor.Current.Returns((ClaimsPrincipal?)null);

    protected void SetupNoUser() => _currentPrincipalAccessor.Current.Returns((ClaimsPrincipal?)null);
}
