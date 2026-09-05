// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_DefaultKeyForCommandResolver.when_resolving;

public class and_the_marked_key_is_a_concept : Specification
{
    DefaultKeyForCommandResolver _resolver;
    RenameCustomerByConcept _command;
    string? _result;

    void Establish()
    {
        _resolver = new();
        _command = new(new CustomerId(Guid.NewGuid()), "Alice");
    }

    void Because() => _result = _resolver.Resolve(_command);

    [Fact] void should_resolve_the_value_the_concept_wraps() => _result.ShouldEqual(_command.CustomerId.Value.ToString());
}
