// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_GeneratedValueParameters.when_describing;

public class without_a_factory_or_dynamic_code : given.generated_parameter_declarations
{
    void Because() => _error = Catch.Exception(() => GeneratedValueParameters.Describe(
        typeof(ExplicitImplementation), typeof(ExplicitImplementation).GetMethod("Handle")!, dynamicCodeSupported: false));

    [Fact] void should_refuse_reflection_at_startup() => _error.ShouldBeOfExactType<MissingGeneratedValueFactory>();
    [Fact] void should_name_the_command() => _error.Message.ShouldContain(typeof(ExplicitImplementation).FullName!);
    [Fact] void should_name_the_parameter() => _error.Message.ShouldContain("id");
    [Fact] void should_explain_the_fix() => _error.Message.ShouldContain("source generator");
}
