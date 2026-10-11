// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValueParameters.when_describing;

public class with_a_non_generatable_type : given.generated_parameter_declarations
{
    void Because() => _error = Catch.Exception(() => HandlerFor<NonGeneratable>());

    [Fact] void should_fail_at_handler_construction() => _error.ShouldBeOfExactType<InvalidGeneratedValueParameter>();
    [Fact] void should_name_the_command() => _error.Message.ShouldContain(typeof(NonGeneratable).FullName!);
    [Fact] void should_name_the_parameter() => _error.Message.ShouldContain("id");
}
