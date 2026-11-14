// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_mapping_a_protocol_only_command : Specification
{
    a_recording_endpoint_mapper _mapper;

    void Because()
    {
        var providers = Substitute.For<ICommandHandlerProviders>();
        providers.Handlers.Returns([
            Handler(typeof(ProtocolCommand)),
            Handler(typeof(ExplicitProtocolCommand)),
            Handler(typeof(PublicCommand))
        ]);
        _mapper = new a_recording_endpoint_mapper();
        var options = new ArcOptions();
        options.GeneratedApis.IncludeCommandNameInRoute = false;
        options.GeneratedApis.SegmentsToSkipForRoute = 1;
        _mapper.MapCommandEndpoints(new ServiceCollection()
            .AddSingleton(providers)
            .AddSingleton(Options.Create(options))
            .BuildServiceProvider());
    }

    [Fact] void should_only_map_the_public_commands_routes() => _mapper.Mapped.Count.ShouldEqual(2);
    [Fact] void should_not_include_protocol_only_siblings_in_route_disambiguation() => _mapper.Mapped[0].Pattern.ShouldEqual("/api/orders");
    [Fact] void should_only_map_validation_for_the_public_command() => _mapper.Mapped[1].Pattern.ShouldEqual("/api/orders/validate");

    static ICommandHandler Handler(Type type)
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(type);
        handler.Location.Returns(["App", "Orders"]);
        return handler;
    }

    [Command]
    [ProtocolOnly]
    public record ProtocolCommand
    {
        public void Handle() { }
    }

    [Command]
    [ProtocolOnly]
    [Path("/protocol/explicit")]
    public record ExplicitProtocolCommand
    {
        public void Handle() { }
    }

    [Command]
    public record PublicCommand
    {
        public void Handle() { }
    }
}
