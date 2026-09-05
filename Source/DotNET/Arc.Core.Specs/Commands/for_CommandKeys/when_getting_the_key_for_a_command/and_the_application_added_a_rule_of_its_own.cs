// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandKeys.when_getting_the_key_for_a_command;

/// <summary>
/// The rule Arc ships is asked last however the rules are discovered, so the application's rule decides either way round.
/// </summary>
public class and_the_application_added_a_rule_of_its_own : Specification
{
    string? _arcShippedRuleLast;
    string? _arcShippedRuleFirst;

    void Because()
    {
        var command = new RenameCustomer(Guid.NewGuid(), "Alice");
        _arcShippedRuleLast = Keys([new an_application_rule("from-the-application"), new DefaultKeyForCommandResolver()]).GetKeyFor(command);
        _arcShippedRuleFirst = Keys([new DefaultKeyForCommandResolver(), new an_application_rule("from-the-application")]).GetKeyFor(command);
    }

    [Fact] void should_let_the_application_rule_decide() => _arcShippedRuleLast.ShouldEqual("from-the-application");
    [Fact] void should_let_it_decide_whichever_order_the_rules_are_discovered_in() => _arcShippedRuleFirst.ShouldEqual("from-the-application");

    static CommandKeys Keys(IEnumerable<ICanResolveKeyForCommand> resolvers)
    {
        var instances = Substitute.For<IInstancesOf<ICanResolveKeyForCommand>>();
        instances.GetEnumerator().Returns(_ => resolvers.GetEnumerator());
        return new(instances);
    }
}
