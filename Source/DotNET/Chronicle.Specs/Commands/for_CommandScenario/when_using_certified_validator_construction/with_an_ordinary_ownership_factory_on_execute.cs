// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_an_ordinary_ownership_factory_on_execute : given.an_ordinary_ownership_factory
{
    CommandResult _result;

    async Task Because() => _result = await _pipeline.Execute(_command, _scope.ServiceProvider, ValidationResultSeverity.Error);

    [Fact]
    void should_refuse_an_ordinary_factory_without_executing_or_silently_dropping_its_rules()
    {
        _result.ExceptionMessages.Single().ShouldContain("Discoverable validator");
        _calls.ShouldEqual(0);
        _scenario.AppendedEvents.ShouldBeEmpty();
    }
}
