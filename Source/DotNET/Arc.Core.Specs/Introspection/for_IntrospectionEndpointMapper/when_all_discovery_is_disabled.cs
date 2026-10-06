// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_all_discovery_is_disabled : Specification
{
    IEndpointMapper _inner;
    GuardedMapper _mapper;
    ServiceProvider _services;
    IntrospectionOptions _options;

    void Establish()
    {
        _inner = Substitute.For<IEndpointMapper>();
        _mapper = new GuardedMapper(_inner);
        _options = new IntrospectionOptions { Enabled = false, IdentityDiscovery = false, RequireAuthentication = false };
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<ArcOptions>>(Options.Create(new ArcOptions { Introspection = _options }));
        _services = services.BuildServiceProvider();
    }

    void Because()
    {
        _mapper.MapIntrospectionEndpoints(_options);
        _mapper.MapIdentityProviderEndpoint(_services);
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_not_map_discovery() => _inner.DidNotReceive().MapGet(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>());
    [Fact] void should_not_resolve_exposure() => _mapper.ServiceResolutions.ShouldEqual(0);
    [Fact] void should_not_check_authentication_enforcement() => _mapper.EnforcementChecks.ShouldEqual(0);
    [Fact] void should_not_defer_mapping() => _mapper.DeferredMappings.ShouldEqual(0);

    sealed class GuardedMapper(IEndpointMapper inner) : IEndpointMapper, IIntrospectionExposureGuard
    {
        public int ServiceResolutions { get; private set; }
        public int EnforcementChecks { get; private set; }
        public int DeferredMappings { get; private set; }
        public IServiceProvider? Services
        {
            get
            {
                ServiceResolutions++;
                return null;
            }
        }

        public string? FindEnforcementProblem(IServiceProvider? services)
        {
            EnforcementChecks++;
            return "No authentication scheme.";
        }

        public bool TryDeferMapping(Action<IServiceProvider> mapping)
        {
            DeferredMappings++;
            return false;
        }

        public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => inner.MapGet(pattern, handler, metadata);
        public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => inner.MapPost(pattern, handler, metadata);
        public void MapMethod(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => inner.MapMethod(httpMethod, pattern, handler, metadata);
        public bool EndpointExists(string name) => inner.EndpointExists(name);
    }
}
