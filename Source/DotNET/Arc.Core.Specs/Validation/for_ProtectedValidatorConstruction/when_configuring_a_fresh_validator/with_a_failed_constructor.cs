// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_a_failed_constructor : for_DiscoverableValidators.when_using_a_certified_provider.given.a_certified_validator
{
    Exception _error;

    void Establish()
    {
        _types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(FailingValidator)]);
        _validators = new(_types);
        ProtectedDecisionRegistrationOrigins.For(_services).DiscoverValidators([typeof(FailingValidator)]);
    }

    async Task Because()
    {
        Build();
        var graph = new ModelGraphValidator(_validators, new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance));
        _error = await Catch.Exception(() => graph.Validate(new(new FailingModel(), _scope.ServiceProvider)));
    }

    [Fact] void should_preserve_the_constructor_failure() => _error.Message.ShouldEqual("constructor failed");
    [Fact] void should_release_completed_dependencies_even_when_the_validator_never_finished() => _events.ShouldEqual(["fresh constructed", "fresh disposed"]);

    public record FailingModel;

    public class FailingValidator : CommandValidator<FailingModel>
    {
        public FailingValidator(Fresh fresh) => throw new InvalidOperationException("constructor failed");
    }
}
