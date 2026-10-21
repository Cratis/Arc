// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelKeys;

public class when_a_type_is_not_registered : Specification
{
    record Unregistered(string Name);

    bool _commandFound;
    bool _modelFound;

    void Because()
    {
        _commandFound = CommandReadModelKeys.TryGetProperties(typeof(Unregistered), out _);
        _modelFound = CommandReadModelKeys.TryGetKeyType(typeof(Unregistered), out _);
    }

    [Fact] void should_not_find_the_command() => _commandFound.ShouldBeFalse();
    [Fact] void should_not_find_the_model() => _modelFound.ShouldBeFalse();
}
