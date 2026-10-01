// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.when_resolving.given;

public class a_host : Specification
{
    protected IEndpointMapper _mapper;
    protected IEndpointMapper _mapperThatCannotAuthenticate;
    internal DiscoveryAccess _access;
    protected Exception? _failure;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _mapperThatCannotAuthenticate = new MapperWithoutAuthentication(Substitute.For<IEndpointMapper>());
    }

    protected void Resolve(IEndpointMapper mapper, IntrospectionOptions options, bool isDevelopment) =>
        _failure = Catch.Exception(() => _access = DiscoveryExposure.Resolve(mapper, options, isDevelopment));

    sealed class MapperWithoutAuthentication(IEndpointMapper inner) : IEndpointMapper, IIntrospectionExposureGuard
    {
        public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => inner.MapGet(pattern, handler, metadata);
        public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => inner.MapPost(pattern, handler, metadata);
        public void MapMethod(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => inner.MapMethod(httpMethod, pattern, handler, metadata);
        public bool EndpointExists(string name) => inner.EndpointExists(name);
        public string? FindEnforcementProblem(IServiceProvider? services) => "No authentication scheme.";
    }
}
