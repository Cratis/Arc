// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.EndpointGroups;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilityGatedGroupPort.given;

/// <summary>
/// The real capability gate in front of real endpoint routing and the real endpoint group matcher policy.
/// There is no socket: the local port is set on the connection, which is the value the policy and the gate read.
/// </summary>
public class a_routed_capability_pipeline : Specification
{
    protected const int CapabilitiesPort = 42001;
    protected int _defaultGroupHandlerCalls;
    protected int _capabilityGroupHandlerCalls;
    protected DefaultHttpContext _context;
    protected IVerifiedCallerAccessor _verified;
    ServiceProvider _services;
    RequestDelegate _pipeline;

    void Establish()
    {
        var arc = new ArcOptions();
        arc.EndpointGroups.Assign("capabilities", CapabilitiesPort);
        var capabilities = new CapabilitiesOptions();
        var gate = new CapabilityGateMiddleware(Options.Create(arc), Options.Create(capabilities), NullLogger<CapabilityGateMiddleware>.Instance, new CapabilityEndpointSelection(Options.Create(capabilities)));

        _verified = Substitute.For<IVerifiedCallerAccessor>();
        _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));
        _services = new ServiceCollection()
            .AddLogging()
            .AddRouting()
            .AddSingleton(new System.Diagnostics.DiagnosticListener("Cratis.Arc.Specs.CapabilityGatedGroupPort"))
            .AddSingleton<System.Diagnostics.DiagnosticSource>(sp => sp.GetRequiredService<System.Diagnostics.DiagnosticListener>())
            .AddAuthenticationCore()
            .AddSingleton(_verified)
            .AddSingleton<MatcherPolicy>(new EndpointGroupMatcherPolicy(Options.Create(arc)))
            .BuildServiceProvider();

        var app = new ApplicationBuilder(_services);
        app.Use((context, next) => gate.InvokeAsync(context, _ => next(context)));
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            // Arc's endpoint mapper marks every default-group endpoint with a null group name.
            endpoints.MapGet("/default", () => _defaultGroupHandlerCalls++)
                .WithMetadata(new EndpointGroupMetadata(null));
            endpoints.MapGet("/capability", () => _capabilityGroupHandlerCalls++)
                .WithMetadata(new EndpointGroupMetadata("capabilities"));
        });
        _pipeline = app.Build();

        _context = new DefaultHttpContext { RequestServices = _services };
        _context.Connection.LocalPort = CapabilitiesPort;
        _context.Request.Method = HttpMethods.Get;
        _context.Response.Body = new MemoryStream();
    }

    protected Task Send(string path)
    {
        _context.Request.Path = path;
        return _pipeline(_context);
    }

    protected void CallerIsVerified() =>
        _verified.GetCurrent().Returns(new CallerResolution(
            new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null),
            null));

    void Destroy() => _services.Dispose();
}
