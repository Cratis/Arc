// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelKeys;

public class when_registering_a_command_twice : Specification
{
    record Twice(string Name);

    CommandReadModelKeyProperty[] _properties;

    void Because()
    {
        CommandReadModelKeys.RegisterCommand(typeof(Twice), new CommandReadModelKeyProperty("First", typeof(string), _ => "first"));
        CommandReadModelKeys.RegisterCommand(typeof(Twice), new CommandReadModelKeyProperty("Second", typeof(string), _ => "second"));
        CommandReadModelKeys.TryGetProperties(typeof(Twice), out _properties);
    }

    [Fact] void should_keep_the_first_registration() => _properties.Select(property => property.Name).ShouldEqual(["First"]);
}
