// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_the_public_api_cannot_activate_a_runtime_validator : given.a_validator_runtime_application
{
    void Establish() => File.WriteAllText(Path.Combine(Path.GetDirectoryName(ApplicationPath)!, "throw-activation"), string.Empty);

    Task Because() => RunInFreshProcess(HarnessPath, ApplicationPath, typeof(Specs.ReferencedConceptCommand.CommandUsingReferencedEmail).Assembly.Location, OutputPath);

    [Fact] void should_complete_the_isolated_verification() => ExitCode.ShouldEqual(0);
    [Fact] void should_return_false_with_the_activation_cause() => Output.ShouldContain("public API returned false with the activation cause");
    [Fact] void should_throw_instead_of_extracting_empty_rules() => Output.ShouldContain("extractor failed closed");
    [Fact] void should_generate_successfully_after_a_failed_run() => Output.ShouldContain("generated successfully after failure");
    [Fact] void should_release_the_metadata_context_after_failure() => Output.ShouldContain("metadata context released after failure");
    [Fact] void should_release_the_metadata_context_after_repeated_initialization() => Output.ShouldContain("metadata context released after success");
    [Fact] void should_not_update_indexes_after_failure() => File.ReadAllText(Path.Combine(OutputPath, "index.ts")).ShouldEqual("unchanged index");
}
