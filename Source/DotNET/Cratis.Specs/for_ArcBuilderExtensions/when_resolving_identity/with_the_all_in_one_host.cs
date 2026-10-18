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
public class with_the_all_in_one_host : given.a_chronicle_web_host
{
    IChronicleBuilder _chronicle;
    ArcIdentityProvider _provider;
    Identity _offRequest;
    Identity _http;

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
            configureChronicleBuilder: chronicle =>
            {
                _chronicle = chronicle;
                chronicle.WithArtifactsProvider(Substitute.For<IClientArtifactsProvider>());
            });
        _host = builder.Build();
    }

    void Because()
    {
        _host.Services.GetRequiredService<IChronicleClient>();
        _provider = _host.Services.GetRequiredService<ArcIdentityProvider>();
        using var scope = _host.Services.GetRequiredService<ISystemExecution>().As(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "worker")], "test")));
        _offRequest = _provider.GetCurrent();
        var http = _host.Services.GetRequiredService<IHttpContextAccessor>();
        try
        {
            http.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "http-user")], "test"))
            };
            using var ignoredOverride = _host.Services.GetRequiredService<ISystemExecution>().As(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "ignored")], "test")));
            _http = _provider.GetCurrent();
        }
        finally
        {
            http.HttpContext = null;
        }
    }

    [Fact] void should_install_arcs_provider_in_the_chronicle_builder() => _chronicle.IdentityProvider.ShouldBeSame(_provider);
    [Fact] void should_resolve_the_scoped_off_request_principal() => _offRequest.ShouldEqual(new Identity("worker", string.Empty));
    [Fact] void should_preserve_http_precedence_over_a_system_execution_scope() => _http.ShouldEqual(new Identity("http-user", string.Empty));
}
