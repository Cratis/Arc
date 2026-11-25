// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_runtime_validator_activation_fails : given.a_validator_runtime_application
{
    void Establish() => File.WriteAllText(Path.Combine(Path.GetDirectoryName(ApplicationPath)!, "throw-activation"), string.Empty);

    Task Because() => GenerateInFreshProcess();

    [Fact] void should_not_save_success_state() => File.Exists(StatePath).ShouldBeFalse();
    [Fact] void should_fail() => ExitCode.ShouldNotEqual(0);
    [Fact] void should_identify_the_validator() => Output.ShouldContain("CreateApplicationValidator");
    [Fact] void should_report_the_activation_cause() => Output.ShouldContain("Fixture validator activation failed");
    [Fact] void should_not_write_the_incomplete_command() => Directory.GetFiles(OutputPath, "CreateApplication.ts", SearchOption.AllDirectories).Length.ShouldEqual(0);
    [Fact] void should_not_update_indexes() => File.ReadAllText(Path.Combine(OutputPath, "index.ts")).ShouldEqual("unchanged index");
}
