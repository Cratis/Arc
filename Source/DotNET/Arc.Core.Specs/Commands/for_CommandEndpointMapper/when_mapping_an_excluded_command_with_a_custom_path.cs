// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_mapping_an_excluded_command_with_a_custom_path : Specification
{
    a_recording_endpoint_mapper _mapper;

    void Because()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(HiddenCommand));
        handler.Location.Returns(["App", "Orders"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        _mapper = new a_recording_endpoint_mapper();
        _mapper.MapCommandEndpoints(new ServiceCollection()
            .AddSingleton(handlers)
            .AddSingleton(Options.Create(new ArcOptions()))
            .BuildServiceProvider());
    }

    [Fact] void should_map_the_execution_route() => _mapper.Mapped[0].Pattern.ShouldEqual("/orders/hidden");
    [Fact] void should_map_the_validation_route() => _mapper.Mapped[1].Pattern.ShouldEqual("/orders/hidden/validate");
    [Fact] void should_exclude_both_api_descriptions() => _mapper.Mapped.TrueForAll(endpoint => endpoint.Metadata?.ExcludeFromApiDescription == true).ShouldBeTrue();

    [ExcludeFromDiscovery]
    [Path("/orders/hidden")]
    public record HiddenCommand;
}
