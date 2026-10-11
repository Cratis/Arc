// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValueParameters.when_describing;

public class with_a_nullable_type : given.generated_parameter_declarations
{
    void Because() => _error = Catch.Exception(() => HandlerFor<Nullable>());

    [Fact] void should_fail_at_handler_construction() => _error.ShouldBeOfExactType<InvalidGeneratedValueParameter>();
    [Fact] void should_explain_nullability() => _error.Message.ShouldContain("non-nullable");
}
