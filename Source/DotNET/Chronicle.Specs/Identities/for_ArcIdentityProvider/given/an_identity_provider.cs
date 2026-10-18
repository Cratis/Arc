// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Chronicle.Identities;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.given;

public class an_identity_provider : Specification
{
    protected IHttpRequestContextAccessor _requests;
    protected IIdentityProvider _fallback;
    protected ICurrentPrincipalAccessor _principals;
    protected ISystemExecution _execution;
    protected ArcIdentityProvider _provider;
    protected ChronicleIdentity _result;
    protected ClaimsPrincipal _principal;

    void Establish()
    {
        _requests = Substitute.For<IHttpRequestContextAccessor>();
        _requests.Current.Returns((IHttpRequestContext?)null);
        var accessor = new CurrentPrincipalAccessor(_requests);
        _principals = accessor;
        _execution = new SystemExecution(accessor);
        _fallback = new BaseIdentityProvider();
        _provider = new(_fallback, _principals, () => _requests.Current is not null);
        _principal = new(new ClaimsIdentity([new Claim("sub", "person-123"), new Claim("name", "Person"), new Claim("preferred_username", "person")], "test"));
    }

    void Destroy() => _fallback.ClearCurrentIdentity();
}
