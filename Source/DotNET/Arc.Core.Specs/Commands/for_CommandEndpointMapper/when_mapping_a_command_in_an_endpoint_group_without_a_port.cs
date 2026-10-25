// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_mapping_a_command_in_an_endpoint_group_without_a_port : Specification
{
    Exception? _error;
    a_recording_endpoint_mapper _mapper;

    void Because()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(InternalCommand));
        handler.Location.Returns(["App", "Orders"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        _mapper = new a_recording_endpoint_mapper();
        _error = Catch.Exception(() => _mapper.MapCommandEndpoints(new ServiceCollection()
            .AddSingleton(handlers)
            .AddSingleton(Options.Create(new ArcOptions()))
            .BuildServiceProvider()));
    }

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<EndpointGroupHasNoPort>();
    [Fact] void should_not_map_any_route() => _mapper.Mapped.ShouldBeEmpty();

    [EndpointGroup("internal")]
    public record InternalCommand;
}
