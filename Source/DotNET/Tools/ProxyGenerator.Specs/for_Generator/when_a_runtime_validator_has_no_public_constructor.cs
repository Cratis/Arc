// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_a_runtime_validator_has_no_public_constructor : given.a_validator_runtime_application
{
    Task Because() => GenerateInFreshProcess("CreateApplication");

    [Fact] void should_fail() => ExitCode.ShouldEqual(1);
    [Fact] void should_identify_the_validator() => Output.ShouldContain("NoPublicConstructorCommandValidator");
    [Fact] void should_report_the_constructor_failure() => Output.ShouldContain("Validator has no public constructor");
    [Fact] void should_not_save_success_state() => File.Exists(StatePath).ShouldBeFalse();
    [Fact] void should_not_write_the_incomplete_command() => Directory.GetFiles(OutputPath, "NoPublicConstructorCommand.ts", SearchOption.AllDirectories).Length.ShouldEqual(0);
    [Fact] void should_not_update_indexes() => File.ReadAllText(Path.Combine(OutputPath, "index.ts")).ShouldEqual("unchanged index");
}
