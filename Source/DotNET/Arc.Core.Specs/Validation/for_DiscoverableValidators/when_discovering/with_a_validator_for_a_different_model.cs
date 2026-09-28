// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_discovering;

public class with_a_validator_for_a_different_model : Specification
{
    record ExpectedModel(string Value);

    class ValidatorForDifferentModel : AbstractValidator<string>, IDiscoverableValidator<ExpectedModel>;

    ITypes _types;
    Exception _error;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ValidatorForDifferentModel)]);
    }

    void Because() => _error = Catch.Exception(() => new DiscoverableValidators(_types).TryGet(typeof(ExpectedModel), Substitute.For<IServiceProvider>(), out _));

    [Fact] void should_reject_the_mismatched_validator() => _error.ShouldBeOfExactType<DiscoverableValidatorMustImplementAbstractValidator>();
}
