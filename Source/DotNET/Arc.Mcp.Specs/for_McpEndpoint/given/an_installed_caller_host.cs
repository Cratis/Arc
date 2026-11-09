// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public class an_installed_caller_host : an_mcp_host
{
    protected McpResponse _pat;
    protected McpResponse _oauth;
    protected McpResponse _anonymous;
    protected McpResponse _patEndpoint;
    protected McpResponse _oauthEndpoint;

    protected override void ConfigureCallers(IServiceCollection services)
    {
        var pat = Source(PresentedCredentialShape.PatBearer, CallerCredentialKind.PersonalAccessToken);
        var oauth = Source(PresentedCredentialShape.OAuthBearer, CallerCredentialKind.OAuthGrant);
        var installation = new VerifiedCallersBuilder()
            .ForResource("https://resource.example", "audience", "test")
            .UsePersonDirectory<PersonDirectory>();
        installation.InstallPersonalAccessTokens(services, []);
        services.AddScoped<IVerifiedCredentialSource>(_ => pat);
        services.AddScoped<IVerifiedCredentialSource>(_ => oauth);
    }

    protected override void ConfigureServices(IServiceCollection services) =>
        services.Configure<CapabilitiesOptions>(options => options.GatedEndpoints!.ForRoutePattern("/pat-only"));

    protected override void ConfigureEndpoints(WebApplication app) =>
        app.MapPost("/pat-only", () => "admitted");

    protected async Task CallPresentations()
    {
        _pat = await ListTools(request => request.Headers.TryAddWithoutValidation("Authorization", "Bearer arc_pat_spec"));
        _oauth = await ListTools(request => request.Headers.TryAddWithoutValidation("Authorization", "Bearer oauth-spec"));
        _anonymous = await ListTools();
        _patEndpoint = await SendToPatEndpoint("arc_pat_spec");
        _oauthEndpoint = await SendToPatEndpoint("oauth-spec");
    }

    static VerifiedCredentialSourceForSpecs Source(PresentedCredentialShape shape, CallerCredentialKind kind)
    {
        var source = Substitute.For<VerifiedCredentialSourceForSpecs>();
        source.Shape.Returns(shape);
        var facts = new VerifiedCredentialFacts(CallerPrincipalKind.Person, kind, new("credential"), new("https://resource.example", "audience", "test"))
        {
            AccountKey = new("https://accounts.example.test", "12345678-1234-4234-8234-123456789abc"),
            Person = new("person"),
            Assurance = new("https://accounts.example.test", 1, null, null),
            Scopes = new HashSet<string> { ReadScope, WriteScope }
        };
        source.Verify(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(CredentialVerification.Verified(facts)));
        return source;
    }

    async Task<McpResponse> SendToPatEndpoint(string credential)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{_capabilityPort}/pat-only");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {credential}");
        using var response = await _client.SendAsync(request, CancellationToken);
        return new(response.StatusCode, response.Headers.WwwAuthenticate.ToString(), response.Content.Headers.ContentType?.MediaType, await response.Content.ReadAsStringAsync(CancellationToken));
    }
}
