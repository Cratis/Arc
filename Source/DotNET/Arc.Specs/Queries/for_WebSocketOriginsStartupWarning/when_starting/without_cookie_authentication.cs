// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_WebSocketOriginsStartupWarning.when_starting;

public class without_cookie_authentication : given.a_startup_warning
{
    void Establish()
    {
        _schemes.Add(new("Bearer", null, typeof(OtherHandler)));
    }

    Task Because() => Start();

    [Fact] void should_not_warn() => _levels.Count(_ => _ == LogLevel.Warning).ShouldEqual(0);

    class OtherHandler : Microsoft.AspNetCore.Authentication.IAuthenticationHandler
    {
        public Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync() => throw new NotSupportedException();

        public Task ChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties? properties) => throw new NotSupportedException();

        public Task ForbidAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties? properties) => throw new NotSupportedException();

        public Task InitializeAsync(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Http.HttpContext context) => throw new NotSupportedException();
    }
}
