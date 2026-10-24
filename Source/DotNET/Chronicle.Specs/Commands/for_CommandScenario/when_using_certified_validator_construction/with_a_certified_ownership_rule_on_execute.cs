// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_a_certified_ownership_rule_on_execute : given.a_certified_ownership_rule
{
    CommandResult _result;

    async Task Because() => _result = await _pipeline.Execute(_command, _scope.ServiceProvider);

    [Fact]
    void should_keep_the_arc_configured_ownership_denial_on_execute()
    {
        _result.ValidationResults.Single().Message.ShouldEqual("ownership denied");
        _calls.ShouldEqual(1);
        _scenario.AppendedEvents.ShouldBeEmpty();
    }
}
