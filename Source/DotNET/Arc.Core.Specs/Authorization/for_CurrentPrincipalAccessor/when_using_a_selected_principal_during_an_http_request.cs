// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor;

public class when_using_a_selected_principal_during_an_http_request : given.a_current_principal_accessor
{
    ClaimsPrincipal _during;
    ClaimsPrincipal _duringHttp;
    ClaimsPrincipal _after;
    ClaimsPrincipal _afterHttp;
    ClaimsPrincipal _liveUser;

    void Establish()
    {
        SetupHttpRequest();
        _liveUser = _requestUser;
        _httpRequestContext.User.Returns(_ => _liveUser);
        _httpRequestContext.When(context => context.User = Arg.Any<ClaimsPrincipal>())
            .Do(call => _liveUser = call.Arg<ClaimsPrincipal>());
    }

    void Because()
    {
        using (_accessor.UseAuthorizationPrincipal(_systemUser))
        using (_accessor.BeginScope(new ClaimsPrincipal(new ClaimsIdentity("attempted-public-override"))))
        {
            _during = _accessor.Current!;
            _duringHttp = _httpRequestContext.User;
        }

        _after = _accessor.Current!;
        _afterHttp = _httpRequestContext.User;
    }

    [Fact] void should_use_the_selected_principal_in_the_arc_accessor() => _during.ShouldEqual(_systemUser);
    [Fact] void should_use_the_selected_principal_in_the_http_request() => _duringHttp.ShouldEqual(_systemUser);
    [Fact] void should_restore_the_original_arc_principal() => _after.ShouldEqual(_requestUser);
    [Fact] void should_restore_the_original_http_principal() => _afterHttp.ShouldEqual(_requestUser);
}
