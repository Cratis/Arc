// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Introspection;

namespace Cratis.Arc.Availability.for_AvailabilityDevelopmentEndpoints;

public class when_mapping_in_production : Specification
{
    IEndpointMapper _mapper;

    void Establish() => _mapper = Substitute.For<IEndpointMapper>();

    void Because() => new AvailabilityDevelopmentEndpoints(new DevelopmentAvailability("Production"))
        .Map(_mapper, new DiscoveryEndpointMetadata((name, summary, tag, type) => new EndpointMetadata(name, summary, [tag], AllowAnonymous: true, ResponseType: type)));

    [Fact] void should_map_no_get() => _mapper.DidNotReceive().MapGet(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>());
    [Fact] void should_map_no_post() => _mapper.DidNotReceive().MapPost(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>());
}
