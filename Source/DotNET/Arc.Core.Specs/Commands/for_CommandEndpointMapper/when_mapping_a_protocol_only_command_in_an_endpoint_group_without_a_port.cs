// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_mapping_a_protocol_only_command_in_an_endpoint_group_without_a_port : Specification
{
    Exception? _error;
    a_recording_endpoint_mapper _mapper;

    void Because()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(ProtocolCommand));
        handler.Location.Returns(["App", "Orders"]);
        var providers = Substitute.For<ICommandHandlerProviders>();
        providers.Handlers.Returns([handler]);
        _mapper = new a_recording_endpoint_mapper();
        _error = Catch.Exception(() => _mapper.MapCommandEndpoints(new ServiceCollection()
            .AddSingleton(providers)
            .AddSingleton(Options.Create(new ArcOptions()))
            .BuildServiceProvider()));
    }

    [Fact] void should_not_require_a_served_endpoint_group() => _error.ShouldBeNull();
    [Fact] void should_not_map_any_route() => _mapper.Mapped.ShouldBeEmpty();

    [Command]
    [ProtocolOnly]
    [EndpointGroup("internal")]
    public record ProtocolCommand
    {
        public void Handle() { }
    }
}
