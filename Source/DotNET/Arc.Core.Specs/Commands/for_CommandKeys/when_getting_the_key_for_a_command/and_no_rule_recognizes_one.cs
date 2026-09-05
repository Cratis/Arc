// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandKeys.when_getting_the_key_for_a_command;

public class and_no_rule_recognizes_one : Specification
{
    CommandKeys _keys;
    string? _result;

    void Establish()
    {
        var instances = Substitute.For<IInstancesOf<ICanResolveKeyForCommand>>();
        instances.GetEnumerator().Returns(_ => new List<ICanResolveKeyForCommand> { new an_application_rule(null) }.GetEnumerator());
        _keys = new(instances);
    }

    void Because() => _result = _keys.GetKeyFor(new RenameCustomer(Guid.NewGuid(), "Alice"));

    [Fact] void should_resolve_nothing() => _result.ShouldBeNull();
}
