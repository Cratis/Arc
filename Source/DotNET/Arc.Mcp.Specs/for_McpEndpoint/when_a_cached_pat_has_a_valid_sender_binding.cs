// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_cached_pat_has_a_valid_sender_binding : given.an_mcp_host
{
    McpResponse _response;

    protected override void ConfigureCallers(IServiceCollection services)
    {
        base.ConfigureCallers(services);
        services.AddSingleton(new PatCapableCallerInstallation());
        services.AddSingleton(new OAuthCapableCallerInstallation());
        services.AddSingleton<CredentialPresentationGate>();
        var source = Substitute.For<VerifiedCredentialSourceForSpecs>();
        source.Shape.Returns(PresentedCredentialShape.PatBearer);
        services.AddScoped<IVerifiedCredentialSource>(_ => source);
        var caller = VerifiedCallerFor(CallerCredentialKind.PersonalAccessToken, "tenant-a", ReadScope) with { SenderBinding = new("key", "proof-id") };
        _verified.GetCurrent().Returns(new CallerResolution(caller, null));
    }

    async Task Because() => _response = await ListTools(request => request.Headers.TryAddWithoutValidation("Authorization", "Bearer direct_pat_fixture"));

    [Fact] void should_refuse_the_cached_binding() => _response.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_dispatch_any_tool() => _probe.Count.ShouldEqual(0);
}
