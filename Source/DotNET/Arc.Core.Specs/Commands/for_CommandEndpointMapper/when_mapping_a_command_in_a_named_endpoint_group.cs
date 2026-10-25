// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_mapping_a_command_in_a_named_endpoint_group : Specification
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
        options.EndpointGroups.Assign("internal", 5001);
        _mapper = new a_recording_endpoint_mapper();
        _mapper.MapCommandEndpoints(new ServiceCollection()
            .AddSingleton(handlers)
            .AddSingleton(Options.Create(options))
            .BuildServiceProvider());
    }

    [Fact] void should_place_both_routes_in_the_group() => _mapper.Mapped.TrueForAll(endpoint => endpoint.Metadata?.EndpointGroup == "internal").ShouldBeTrue();
    [Fact] void should_exclude_both_api_descriptions() => _mapper.Mapped.TrueForAll(endpoint => endpoint.Metadata?.ExcludeFromApiDescription == true).ShouldBeTrue();

    [EndpointGroup("internal")]
    public record InternalCommand;
}
