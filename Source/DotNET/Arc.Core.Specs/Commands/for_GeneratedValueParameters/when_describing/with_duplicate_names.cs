// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValueParameters.when_describing;

public class with_duplicate_names : given.generated_parameter_declarations
{
    void Because() => _error = Catch.Exception(() => HandlerFor<DuplicateNames>());

    [Fact] void should_fail_at_handler_construction() => _error.ShouldBeOfExactType<InvalidGeneratedValueParameter>();
    [Fact] void should_explain_name_uniqueness() => _error.Message.ShouldContain("unique");
}
