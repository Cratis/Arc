// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_a_command_whose_validator_cannot_be_constructed : given.a_runtime_dependency_application
{
    Task Because() => Generate("CreateApplication");

    [Fact] void should_fail_generation() => ExitCode.ShouldEqual(1);
    [Fact] void should_report_an_error() => StandardError.ShouldContain("error:");
    [Fact] void should_name_the_validator() => StandardError.ShouldContain("RenameApplicationValidator");
    [Fact] void should_report_the_root_cause() => StandardError.ShouldContain("Rename application validator cannot be constructed");
    [Fact] void should_not_write_the_command() => Directory.GetFiles(OutputPath, "RenameApplication.ts", SearchOption.AllDirectories).ShouldBeEmpty();
}
