// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_DefaultKeyForCommandResolver.when_resolving;

public class and_the_command_composes_its_own_key : Specification
{
    DefaultKeyForCommandResolver _resolver;
    MoveItem _command;
    string? _result;

    void Establish()
    {
        _resolver = new();
        _command = new(Guid.NewGuid(), Guid.NewGuid());
    }

    void Because() => _result = _resolver.Resolve(_command);

    [Fact] void should_resolve_the_composed_key() => _result.ShouldEqual($"{_command.CartId}/{_command.ItemId}");
}
