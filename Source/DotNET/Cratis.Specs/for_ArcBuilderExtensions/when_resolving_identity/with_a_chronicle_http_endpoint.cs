// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Identities;
using Cratis.Chronicle;
using Cratis.Chronicle.Identities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_identity;

[Collection("UsesCurrentDirectory")]
public class with_a_chronicle_http_endpoint : given.a_chronicle_web_host
{
    Identity _identity;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratis(
            configureArcBuilder: arc => arc.Services.AddSingleton(Substitute.For<Cratis.Chronicle.Connections.IChronicleConnection>()),
            configureChronicleOptions: options =>
            {
                options.EventStore = EventStore;
                options.AutoDiscoverAndRegister = false;
            },
            configureChronicleBuilder: chronicle => chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>()));
        _host = builder.Build();
    }

    void Because()
    {
        _host.Services.GetRequiredService<IChronicleClient>();
        using var scope = _host.Services.GetRequiredService<ISystemExecution>().As(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "worker")], "test")));
        var http = _host.Services.GetRequiredService<IHttpContextAccessor>();
        try
        {
            // Exercise HTTP identity resolution before Arc's request-context middleware has run.
            http.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "http-user")], "test"))
            };
            http.HttpContext.Request.Path = "/.cratis/events";
            _identity = _host.Services.GetRequiredService<ArcIdentityProvider>().GetCurrent();
        }
        finally
        {
            http.HttpContext = null;
        }
    }

    [Fact] void should_preserve_chronicles_internal_endpoint_identity() => _identity.ShouldEqual(Identity.System);
}
