// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_a_failed_callback : for_DiscoverableValidators.when_using_a_certified_provider.given.a_certified_validator
{
    readonly Exception _callbackFailure = new InvalidOperationException("callback failed");
    Exception _error;

    void Establish() => _services.AddProtectedDecisionValidator<ReadingValidator>(_ => throw _callbackFailure);

    async Task Because()
    {
        Build();
        var graph = new ModelGraphValidator(_validators, new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance));
        _error = await Catch.Exception(() => graph.Validate(new(new Model(), _scope.ServiceProvider)));
    }

    [Fact] void should_not_convert_construction_failure_to_a_filterable_validation_result() => _error.ShouldBeSame(_callbackFailure);
    [Fact] void should_release_every_constructed_object() => _events.ShouldEqual(["fresh constructed", "validator constructed", "validator disposed", "fresh disposed"]);
}
