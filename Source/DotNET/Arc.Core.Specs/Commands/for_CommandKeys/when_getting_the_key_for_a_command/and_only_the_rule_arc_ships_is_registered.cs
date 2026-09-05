// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandKeys.when_getting_the_key_for_a_command;

public class and_only_the_rule_arc_ships_is_registered : Specification
{
    CommandKeys _keys;
    RenameCustomer _command;
    string? _result;

    void Establish()
    {
        _command = new(Guid.NewGuid(), "Alice");
        _keys = new(Instances([new DefaultKeyForCommandResolver()]));
    }

    void Because() => _result = _keys.GetKeyFor(_command);

    [Fact] void should_resolve_the_marked_property() => _result.ShouldEqual(_command.CustomerId.ToString());

    static IInstancesOf<ICanResolveKeyForCommand> Instances(IEnumerable<ICanResolveKeyForCommand> resolvers)
    {
        var instances = Substitute.For<IInstancesOf<ICanResolveKeyForCommand>>();
        instances.GetEnumerator().Returns(_ => resolvers.GetEnumerator());
        return instances;
    }
}
