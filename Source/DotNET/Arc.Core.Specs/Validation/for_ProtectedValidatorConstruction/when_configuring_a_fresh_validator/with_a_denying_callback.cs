// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_a_denying_callback : for_DiscoverableValidators.when_using_a_certified_provider.given.a_certified_validator
{
    ValidationResult[] _results;
    int _callbacks;

    void Establish() => _services.AddProtectedDecisionValidator<ReadingValidator>(validator =>
    {
        _callbacks++;
        validator.RuleFor(model => model.Name).Must(_ => false).WithMessage("ownership denied");
    });

    async Task Because()
    {
        Build();
        var graph = new ModelGraphValidator(_validators, new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance));
        _results = (await graph.Validate(new(new Model(), _scope.ServiceProvider))).ToArray();
    }

    [Fact] void should_run_configuration_once_on_the_actual_instance() => _callbacks.ShouldEqual(1);
    [Fact] void should_preserve_the_ownership_denial() => _results.Single().Message.ShouldEqual("ownership denied");
    [Fact] void should_dispose_the_validator_before_its_dependencies() => _events.ShouldEqual(["fresh constructed", "validator constructed", "validator disposed", "fresh disposed"]);
}
