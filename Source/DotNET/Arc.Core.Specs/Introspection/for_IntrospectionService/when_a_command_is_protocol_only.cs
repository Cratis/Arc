// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionService;

public class when_a_command_is_protocol_only : Specification
{
    IReadOnlyList<CommandIntrospectionMetadata> _commands;

    void Because()
    {
        var providers = Substitute.For<ICommandHandlerProviders>();
        providers.Handlers.Returns([Handler(typeof(ProtocolCommand)), Handler(typeof(PublicCommand))]);
        var service = new IntrospectionService(
            providers,
            Substitute.For<IQueryPerformerProviders>(),
            Options.Create(new ApiEndpointOptions { IncludeCommandNameInRoute = false, SegmentsToSkipForRoute = 1 }),
            Options.Create(new ArcOptions()));
        _commands = service.Commands;
    }

    [Fact] void should_only_advertise_the_public_command() => _commands.Single().Name.ShouldEqual(nameof(PublicCommand));
    [Fact] void should_not_include_the_protocol_command_in_route_disambiguation() => _commands.Single().Route.ShouldEqual("/api/orders");

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
    public record PublicCommand
    {
        public void Handle() { }
    }
}
