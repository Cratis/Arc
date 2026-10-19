// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_factory_added_ownership_rules : given.a_certified_command
{
    [Fact]
    public async Task should_keep_the_arc_configured_ownership_denial_on_validate_and_execute()
    {
        var calls = 0;
        _scenario.Services.AddProtectedDecisionValidator<CertifiedValidator>(validator =>
        {
            calls++;
            validator.RuleFor(command => command.EventSourceId).Must(_ => false).WithMessage("ownership denied");
        });
        Build();
        var validation = await _pipeline.Validate(_command, _scope.ServiceProvider);
        var execution = await _pipeline.Execute(_command, _scope.ServiceProvider);
        validation.ValidationResults.Single().Message.ShouldEqual("ownership denied");
        execution.ValidationResults.Single().Message.ShouldEqual("ownership denied");
        calls.ShouldEqual(2);
        _scenario.AppendedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_an_ordinary_factory_without_executing_or_silently_dropping_its_rules()
    {
        var calls = 0;
        _scenario.Services.AddTransient<CertifiedValidator>(_ =>
        {
            calls++;
            throw new InvalidOperationException("ownership factory must not run");
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
