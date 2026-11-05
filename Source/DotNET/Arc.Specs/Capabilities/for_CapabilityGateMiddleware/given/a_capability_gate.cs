// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware.given;

public class a_capability_gate : Specification
{
    protected DefaultHttpContext _context;
    protected bool _nextRan;
    protected CapabilityCaller? _caller;
    private protected CapabilityGateMiddleware _gate;
    private protected IVerifiedCallerAccessor _verified;
    private protected VerifiedCaller _person;
    protected ICapabilityCallers _callers;
    ServiceProvider _services;

    void Establish()
    {
        var arc = new ArcOptions();
        arc.EndpointGroups.Assign("capabilities", 42001);
        _gate = new(Options.Create(arc), Options.Create(new CapabilitiesOptions()), NullLogger<CapabilityGateMiddleware>.Instance);
        _context = new DefaultHttpContext();
        _context.Connection.LocalPort = 42001;
        _context.Response.Body = new MemoryStream();
        _verified = Substitute.For<IVerifiedCallerAccessor>();
        _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));
        _services = new ServiceCollection().AddSingleton(_verified).BuildServiceProvider();
        _context.RequestServices = _services;
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(new AspNetCoreHttpRequestContext(_context));
        _callers = new CapabilityCallers(requests);
        _person = new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
    }

    protected Task Invoke() => _gate.InvokeAsync(_context, _ =>
    {
        _nextRan = true;
        _caller = _callers.Current;
        return Task.CompletedTask;
    });

    void Destroy() => _services.Dispose();
}
