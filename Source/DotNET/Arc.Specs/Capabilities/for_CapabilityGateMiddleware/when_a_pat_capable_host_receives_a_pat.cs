// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_a_pat_capable_host_receives_a_pat : given.a_capability_gate
{
    protected override void Configure(IServiceCollection services)
    {
        services.AddSingleton(new PatCapableCallerInstallation());
        services.AddSingleton<CredentialPresentationGate>();
    }

    void Establish()
    {
        _context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/mcp";
        _context.Request.Headers.Authorization = "Bearer direct_pat_example";
        _verified.GetCurrent().Returns(new CallerResolution(_person, null));
    }

    Task Because() => Invoke();

    [Fact] void should_continue_after_verification() => _nextRan.ShouldBeTrue();
    [Fact] void should_publish_the_checked_caller() => _caller!.CredentialId.ShouldEqual(_person.CredentialId);
}
