// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_a_cached_pat_has_a_valid_sender_binding : given.a_capability_gate
{
    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton(new PatCapableCallerInstallation());
        services.AddSingleton(new OAuthCapableCallerInstallation());
        services.AddSingleton<CredentialPresentationGate>();
    }

    void Establish()
    {
        _context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/rest";
        _context.Request.Headers.Authorization = "Bearer direct_pat_example";
        _verified.GetCurrent().Returns(new CallerResolution(_person with { SenderBinding = new("key", "proof-id") }, null));
    }

    Task Because() => Invoke();

    [Fact] void should_not_dispatch() => _nextRan.ShouldBeFalse();
    [Fact] void should_refuse_the_cached_binding() => _context.Response.StatusCode.ShouldEqual(401);
    [Fact] void should_not_publish_a_capability_caller() => _caller.ShouldBeNull();
}
