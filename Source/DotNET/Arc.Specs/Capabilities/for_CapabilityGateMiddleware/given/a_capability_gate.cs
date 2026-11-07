// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware.given;

public class a_capability_gate : Specification
{
    protected DefaultHttpContext _context;
    protected bool _nextRan;
    protected CapabilityCaller? _caller;
    protected List<string> _logged;
    private protected CapabilityGateMiddleware _gate;
    private protected IVerifiedCallerAccessor _verified;
    private protected VerifiedCaller _person;
    protected ICapabilityCallers _callers;
    ServiceProvider _services;

    void Establish()
    {
        var arc = new ArcOptions();
        arc.EndpointGroups.Assign("capabilities", 42001);
        _logged = new();
        _gate = new(Options.Create(arc), Options.Create(new CapabilitiesOptions()), new CapturingLogger(_logged), new CapabilityEndpointSelection(Options.Create(new CapabilitiesOptions())));
        _context = new DefaultHttpContext();
        _context.Connection.LocalPort = 42001;
        _context.Response.Body = new MemoryStream();
        _verified = Substitute.For<IVerifiedCallerAccessor>();
        _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));
        var services = new ServiceCollection().AddLogging().AddSingleton(_verified);
        services.AddAuthenticationCore();
        Configure(services);
        _services = services.BuildServiceProvider();
        _context.RequestServices = _services;
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(new AspNetCoreHttpRequestContext(_context));
        _callers = new CapabilityCallers(requests);
        _person = new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
    }

    protected virtual void Configure(IServiceCollection services)
    {
    }

    protected Task Invoke() => _gate.InvokeAsync(_context, _ =>
    {
        _nextRan = true;
        _caller = _callers.Current;
        return Task.CompletedTask;
    });

    void Destroy() => _services.Dispose();

    sealed class CapturingLogger(List<string> logged) : ILogger<CapabilityGateMiddleware>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logged.Add(formatter(state, exception));
    }
}
