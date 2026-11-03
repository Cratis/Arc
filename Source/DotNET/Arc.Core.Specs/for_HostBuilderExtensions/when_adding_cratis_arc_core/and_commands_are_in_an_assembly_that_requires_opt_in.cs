// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.for_HostBuilderExtensions.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class and_commands_are_in_an_assembly_that_requires_opt_in : Specification
{
    ServiceProvider _withoutOptIn;
    ServiceProvider _withOptIn;
    Type _command;
    IEnumerable<Type> _withoutOptInHandlers;
    IEnumerable<Type> _withOptInHandlers;

    void Establish()
    {
        var marked = MarkedArtifactHosts.CreateMarkedAssembly();
        _command = marked.Command;
        _withoutOptIn = MarkedArtifactHosts.Build([marked.Command], null);
        _withOptIn = MarkedArtifactHosts.Build([marked.Command], marked.Assembly);
    }

    void Because()
    {
        _withoutOptInHandlers = _withoutOptIn.GetRequiredService<ICommandHandlerProviders>().Handlers.Select(_ => _.CommandType);
        _withOptInHandlers = _withOptIn.GetRequiredService<ICommandHandlerProviders>().Handlers.Select(_ => _.CommandType);
    }

    [Fact] void should_not_serve_the_command_when_the_assembly_is_not_included() => _withoutOptInHandlers.ShouldNotContain(_command);
    [Fact] void should_serve_the_command_when_the_assembly_is_included() => _withOptInHandlers.ShouldContain(_command);

    void Destroy()
    {
        _withoutOptIn.Dispose();
        _withOptIn.Dispose();
    }
}
