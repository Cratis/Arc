// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_applying_endpoint_policy : given.a_signed_request
{
    int _anonymous;
    int _protected;
    int _forbidden;
    int _native;
    ClaimsPrincipal _nativePrincipal;
    ClaimsPrincipal _anonymousPrincipal;

    async Task Because()
    {
        _anonymous = await Apply(new("anonymous", AllowAnonymous: true), false);
        _anonymousPrincipal = _context.User;
        _protected = await Apply(new("protected") { RequireAuthentication = true }, false);
        _forbidden = await Apply(new("roles") { RequireAuthentication = true, Roles = "administrator" }, true);
        _native = await Apply(new("native") { RequireAuthentication = true }, false, native: true);
        _nativePrincipal = _context.User;
    }

    [Fact] void should_keep_anonymous_endpoints_accessible() => _anonymous.ShouldEqual(200);
    [Fact] void should_not_promote_bad_forwarding_on_anonymous_endpoints() => (_anonymousPrincipal.Identity?.IsAuthenticated ?? false).ShouldBeFalse();
    [Fact] void should_challenge_unauthenticated_protected_requests() => _protected.ShouldEqual(401);
    [Fact] void should_forbid_a_valid_identity_without_the_required_role() => _forbidden.ShouldEqual(403);
    [Fact] void should_preserve_an_earlier_native_success() => _native.ShouldEqual(200);
    [Fact] void should_not_augment_native_claims() => _nativePrincipal.Claims.Select(claim => claim.Value).ShouldEqual(["native"]);

    async Task<int> Apply(EndpointMetadata metadata, bool valid, bool native = false)
    {
        ResetRequest();
        _context.User.Returns(new ClaimsPrincipal());
        var status = 200;
        _context.When(context => context.SetStatusCode(Arg.Any<int>())).Do(call => status = call.Arg<int>());
        if (valid)
        {
            Resign(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        else
        {
            _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = ["forged"];
        }
        var signed = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(_options), NullLoggerFactory.Instance);
        var instances = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        var handlers = new List<IAuthenticationHandler>();
        if (native)
        {
            // A concrete handler avoids Castle's unsupported default-interface property interception.
            handlers.Add(new given.a_native_handler());
        }
        handlers.Add(signed);
        instances.GetEnumerator().Returns(_ => handlers.GetEnumerator());
        var authentication = new Cratis.Arc.Authentication.Authentication(instances);
        var middleware = new AuthenticationMiddleware(authentication);
        await middleware.Authenticate(_context, metadata);
        return status;
    }
}
