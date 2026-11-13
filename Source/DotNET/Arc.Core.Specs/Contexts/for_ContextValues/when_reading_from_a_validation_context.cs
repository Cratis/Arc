// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_reading_from_a_validation_context : given.a_provided_vessel
{
    object _value;
    object _withoutValues;

    void Because()
    {
        var context = new ValidationContext<object>("instance");
        context.RootContextData[ValidationContextValuesExtensions.ContextKey] = ContextValues.For(_services, () => typeof(string));
        _value = context.Values().Vessel;
        _withoutValues = new ValidationContext<object>("instance").Values().Vessel;
    }

    [Fact] void should_read_the_provided_value() => _value.ShouldEqual("Aurora");
    [Fact] void should_read_a_value_as_absent_when_the_request_provides_none() => _withoutValues.ShouldBeNull();
}
