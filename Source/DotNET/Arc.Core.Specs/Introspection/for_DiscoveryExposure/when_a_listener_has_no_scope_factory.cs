// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_listener_has_no_scope_factory : Specification
{
    HttpListenerEndpointMapper _mapper;
    string? _problem;

    void Establish() => _mapper = new HttpListenerEndpointMapper(NullLogger<HttpListenerEndpointMapper>.Instance, Substitute.For<IServiceProvider>());

    void Because() => _problem = ((IIntrospectionExposureGuard)_mapper).FindEnforcementProblem(null);

    [Fact] void should_report_unavailable_authentication() => _problem.ShouldEqual("Requiring authentication on the discovery endpoints needs an Arc.Core authentication handler.");

    void Destroy() => _mapper.Dispose();
}
