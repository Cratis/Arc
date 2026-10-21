// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_validator_has_two_lookup_keys : given.a_parameter_lookup
{
    DiscoverableValidators _validators;
    TransferValidator _validator;

    void Establish()
    {
        Configure(new Transfer(new("from"), new("to")));
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(TransferValidator)]);
        _validators = new(types);
    }

    void Because()
    {
        _validators.TryGet(typeof(Transfer), _services, out var validator);
        _validator = (TransferValidator)validator;
    }

    [Fact] void should_inject_the_from_model() => _validator.From.ShouldBeSame(_from);
    [Fact] void should_inject_the_to_model() => _validator.To.ShouldBeSame(_to);
}
