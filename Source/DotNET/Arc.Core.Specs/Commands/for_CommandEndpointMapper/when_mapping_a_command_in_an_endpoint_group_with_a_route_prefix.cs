// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_mapping_a_command_in_an_endpoint_group_with_a_route_prefix : Specification
{
    a_recording_endpoint_mapper _mapper;

    void Because()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(InternalCommand));
        handler.Location.Returns(["App", "Orders"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        var options = new ArcOptions();
        options.GeneratedApis.RoutePrefix = "api";
        options.EndpointGroups.Assign("internal", 5001).SetRoutePrefix("internal", "internal-api");
        _mapper = new a_recording_endpoint_mapper();
        _mapper.MapCommandEndpoints(new ServiceCollection()
            .AddSingleton(handlers)
            .AddSingleton(Options.Create(options))
            .BuildServiceProvider());
    }

    [Fact] void should_map_both_routes() => _mapper.Mapped.Count.ShouldEqual(2);
    [Fact] void should_use_the_group_prefix() => _mapper.Mapped.TrueForAll(e => e.Pattern.StartsWith("/internal-api/", StringComparison.Ordinal)).ShouldBeTrue();

    [EndpointGroup("internal")]
    public record InternalCommand;
}
