// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_DefaultKeyForCommandResolver.when_resolving;

public class and_the_marked_key_is_a_number : Specification
{
    DefaultKeyForCommandResolver _resolver;
    string? _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.Resolve(new RenameCustomerByNumber(42, "Alice"));

    [Fact] void should_resolve_the_number_as_a_string() => _result.ShouldEqual("42");
}
