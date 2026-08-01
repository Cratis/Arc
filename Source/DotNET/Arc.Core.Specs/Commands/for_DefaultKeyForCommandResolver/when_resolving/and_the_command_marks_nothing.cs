// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_DefaultKeyForCommandResolver.when_resolving;

public class and_the_command_marks_nothing : Specification
{
    DefaultKeyForCommandResolver _resolver;
    string? _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.Resolve(new CarriesNoKey("Alice"));

    [Fact] void should_resolve_nothing_rather_than_guess() => _result.ShouldBeNull();
}
