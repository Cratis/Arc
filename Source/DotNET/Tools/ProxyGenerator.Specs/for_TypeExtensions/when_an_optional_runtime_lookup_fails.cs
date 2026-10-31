// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_an_optional_runtime_lookup_fails : for_Generator.given.a_validator_runtime_application
{
    Task Because() => RunInFreshProcess(HarnessPath, ApplicationPath,
        typeof(Specs.ReferencedConceptCommand.CommandUsingReferencedEmail).Assembly.Location, OutputPath, "optional");

    [Fact] void should_complete_the_isolated_verification() => ExitCode.ShouldEqual(0);
    [Fact] void should_preserve_the_optional_lookup_contract() => Output.ShouldContain("optional runtime lookup preserved its fallback contract");
    [Fact] void should_not_prevent_subsequent_generation() => Output.ShouldContain("generated successfully after failure");
}
