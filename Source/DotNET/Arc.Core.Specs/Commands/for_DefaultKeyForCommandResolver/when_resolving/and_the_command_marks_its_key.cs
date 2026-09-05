// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_DefaultKeyForCommandResolver.when_resolving;

public class and_the_command_marks_its_key : Specification
{
    DefaultKeyForCommandResolver _resolver;
    RenameCustomer _command;
    string? _result;

    void Establish()
    {
        _resolver = new();
        _command = new(Guid.NewGuid(), "Alice");
    }

    void Because() => _result = _resolver.Resolve(_command);

    [Fact] void should_resolve_the_marked_property() => _result.ShouldEqual(_command.CustomerId.ToString());
}
