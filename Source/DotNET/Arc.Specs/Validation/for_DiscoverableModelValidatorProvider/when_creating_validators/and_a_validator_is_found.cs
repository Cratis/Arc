// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidatorProvider.when_creating_validators;

public class and_a_validator_is_found : given.a_discoverable_model_validator_provider
{
    IValidator _validator;

    void Establish()
    {
        _validator = Substitute.For<IValidator>();
        _discoverableValidators
            .TryGet(typeof(TestCommand), out Arg.Any<IValidator>())
            .Returns(call =>
            {
                call[1] = _validator;
                return true;
            });
    }

    void Because() => _provider.CreateValidators(_context);

    [Fact] void should_add_a_validator_item() => _context.Results.Count.ShouldEqual(1);
    [Fact] void should_wrap_the_discovered_validator() => _context.Results[0].Validator.ShouldBeOfExactType<DiscoverableModelValidator>();
}
