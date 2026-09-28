// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_discovering;

public class with_a_multi_argument_generic_intermediate_validator : Specification
{
    record Settings(int Limit);
    record TargetModel(string Value);

    abstract class Configured<TSettings, TModel> : DiscoverableValidator<TModel>;

    class ConcreteValidator : Configured<Settings, TargetModel>
    {
        public ConcreteValidator() => RuleFor(model => model.Value).NotEmpty();
    }

    DiscoverableValidators _validators;
    IValidator _validator;
    bool _foundForModel;
    bool _foundForSettings;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ConcreteValidator)]);
        _validators = new DiscoverableValidators(types);
    }

    void Because()
    {
        _foundForModel = _validators.TryGet(typeof(TargetModel), Substitute.For<IServiceProvider>(), out _validator!);
        _foundForSettings = _validators.TryGet(typeof(Settings), Substitute.For<IServiceProvider>(), out _);
    }

    [Fact] void should_register_the_validator_for_its_model() => _foundForModel.ShouldBeTrue();
    [Fact] void should_resolve_the_concrete_validator() => _validator.ShouldBeOfExactType<ConcreteValidator>();
    [Fact] void should_not_register_it_for_the_other_generic_argument() => _foundForSettings.ShouldBeFalse();
}
