// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_discovering;

public class with_a_non_generic_intermediate_validator : Specification
{
    record IntermediateModel(string Value);

    abstract class Intermediate : DiscoverableValidator<IntermediateModel>;

    class ConcreteValidator : Intermediate
    {
        public ConcreteValidator() => RuleFor(model => model.Value).NotEmpty();
    }

    DiscoverableValidators _validators;
    IValidator _validator;
    bool _found;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ConcreteValidator)]);
        _validators = new DiscoverableValidators(types);
    }

    void Because() => _found = _validators.TryGet(typeof(IntermediateModel), Substitute.For<IServiceProvider>(), out _validator!);

    [Fact] void should_find_the_validator_for_its_model() => _found.ShouldBeTrue();
    [Fact] void should_resolve_the_concrete_validator() => _validator.ShouldBeOfExactType<ConcreteValidator>();
}
