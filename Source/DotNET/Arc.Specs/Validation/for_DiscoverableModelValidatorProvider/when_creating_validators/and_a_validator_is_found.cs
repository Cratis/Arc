// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_DiscoverableModelValidatorProvider.when_creating_validators;

public class and_a_validator_is_found : given.a_discoverable_model_validator_provider
{
    void Establish() => _discoverableValidators.HasValidatorFor(typeof(TestCommand)).Returns(true);

    void Because() => _provider.CreateValidators(_context);

    [Fact] void should_add_a_validator_item() => _context.Results.Count.ShouldEqual(1);
    [Fact] void should_add_a_lazy_validator() => _context.Results[0].Validator.ShouldBeOfExactType<DiscoverableModelValidator>();
    [Fact] void should_not_reuse_the_validator() => _context.Results[0].IsReusable.ShouldBeFalse();
    [Fact] void should_not_construct_a_validator() => _discoverableValidators.ReceivedCalls().Count().ShouldEqual(1);
}
