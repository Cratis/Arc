// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_an_overridden_token : given.a_certified_command
{
    [Fact]
    public async Task should_refuse_an_application_open_token_binding_even_when_a_framework_closed_factory_exists()
    {
        _scenario.Services.AddTransient(typeof(DecisionRead<>), typeof(DecisionRead<>));
        Build();
        var result = await _pipeline.Execute(_command, _scope.ServiceProvider);
        result.ExceptionMessages.Single().ShouldContain("Discoverable validator");
        _scenario.AppendedEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_null_and_foreign_token_factories_without_resolving_them(bool returnsNull)
    {
        var calls = 0;
        _scenario.Services.AddTransient<DecisionRead<when_using_decision_mode.DecisionState>>(_ =>
        {
            calls++;
            return returnsNull ? null! : DecisionRead<when_using_decision_mode.DecisionState>.Unprotected((ReadModelKey)"foreign", null);
        });
        Build();
        var validation = await _pipeline.Validate(_command, _scope.ServiceProvider, ValidationResultSeverity.Error);
        var execution = await _pipeline.Execute(_command, _scope.ServiceProvider, ValidationResultSeverity.Error);
        validation.ExceptionMessages.Single().ShouldContain("Discoverable validator");
        execution.ExceptionMessages.Single().ShouldContain("Discoverable validator");
        calls.ShouldEqual(0);
        _scenario.AppendedEvents.ShouldBeEmpty();
    }
}
