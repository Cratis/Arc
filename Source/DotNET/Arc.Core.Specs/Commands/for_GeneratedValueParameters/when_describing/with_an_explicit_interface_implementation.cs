// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValueParameters.when_describing;

public class with_an_explicit_interface_implementation : given.generated_parameter_declarations
{
    object _value;

    void Because()
    {
        var handler = HandlerFor<ExplicitImplementation>();
        _value = GeneratedValueFactories.Create(typeof(ExplicitImplementation), handler.Parameters.Single());
    }

    [Fact] void should_generate_through_the_interface() => _value.ShouldBeOfExactType<Id>();
}
