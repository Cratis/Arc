// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

[PerFactSpecificationLifecycle]
public class when_a_pat_capable_host_receives_browser_headers : given.a_capability_gate
{
    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton(new PatCapableCallerInstallation());
        services.AddSingleton<CredentialPresentationGate>();
    }

    [Theory]
    [InlineData("X-Cratis-Credential-Kind", "browser_session")]
    [InlineData("Cookie", "cratis-identity-session=session")]
    [InlineData("X-Cratis-Forwarded-Identity-Signature", "v2.test.invalid")]
    public async Task should_refuse_without_resolving_or_publishing_a_browser_caller(string header, string value)
    {
        _context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/mcp";
        _context.Request.Headers[header] = value;
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "person")], "unrelated"));
        await Invoke();
        _context.Response.StatusCode.ShouldEqual(401);
        await _verified.DidNotReceive().GetCurrent();
        _context.Items.ContainsKey(CapabilityCallers.ItemKey).ShouldBeFalse();
        _nextRan.ShouldBeFalse();
    }
}
