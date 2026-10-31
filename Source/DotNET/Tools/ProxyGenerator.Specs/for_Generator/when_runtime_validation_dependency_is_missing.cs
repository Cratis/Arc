// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_runtime_validation_dependency_is_missing : given.a_validator_runtime_application
{
    void Establish() => File.Delete(Path.Combine(Path.GetDirectoryName(ApplicationPath)!, "ValidatorRuntimeDependency.dll"));

    Task Because() => GenerateInFreshProcess();

    [Fact] void should_not_save_success_state() => File.Exists(StatePath).ShouldBeFalse();
    [Fact] void should_fail() => ExitCode.ShouldNotEqual(0);
    [Fact] void should_report_validation_discovery_failure() => Output.ShouldContain("Runtime validation discovery failed");
    [Fact] void should_identify_the_dependency() => Output.ShouldContain("ValidatorRuntimeDependency");
    [Fact] void should_not_write_the_incomplete_command() => Directory.GetFiles(OutputPath, "CreateApplication.ts", SearchOption.AllDirectories).Length.ShouldEqual(0);
    [Fact] void should_not_update_indexes() => File.ReadAllText(Path.Combine(OutputPath, "index.ts")).ShouldEqual("unchanged index");
}
