// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Commands.for_CommandRoute;

public class when_resolving_a_derived_command : Specification
{
    string? _route;

    void Because()
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(DerivedCommand));
        _route = CommandRoute.CustomRoute(handler);
    }

    [Fact] void should_not_inherit_the_base_command_path() => _route.ShouldBeNull();

    [Path("/base")]
    public record BaseCommand
    {
        public void Handle() { }
    }

    [Command]
    public record DerivedCommand : BaseCommand;
}
