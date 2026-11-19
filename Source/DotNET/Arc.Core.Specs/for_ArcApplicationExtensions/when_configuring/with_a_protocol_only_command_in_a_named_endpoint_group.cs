// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.EndpointGroups;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcApplicationExtensions.when_configuring;

[Collection("UsesCurrentDirectory")]
public class with_a_protocol_only_command_in_a_named_endpoint_group : given.a_host_without_discovery
{
    Exception? _error;

    void Establish()
    {
        BuildHost();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(ProtocolCommand));
        _app.Services.GetRequiredService<ICommandHandlerProviders>().Handlers.Returns([handler]);
    }

    void Because() => _error = Catch.Exception(() => _app.UseCratisArc());

    [Fact] void should_activate_because_the_command_is_never_exposed_over_http() => _error.ShouldBeNull();

    [Command]
    [ProtocolOnly]
    [EndpointGroup("internal")]
    public record ProtocolCommand
    {
        public void Handle() { }
    }
}
