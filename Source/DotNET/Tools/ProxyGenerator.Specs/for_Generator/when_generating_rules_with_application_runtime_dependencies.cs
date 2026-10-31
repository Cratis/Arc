// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_rules_with_application_runtime_dependencies : given.a_validator_runtime_application
{
    string _command = string.Empty;
    string _plainCommand = string.Empty;

    async Task Because()
    {
        await GenerateInFreshProcess();
        var commandPath = Directory.GetFiles(OutputPath, "CreateApplication.ts", SearchOption.AllDirectories).SingleOrDefault();
        _command = commandPath is null ? Output : await File.ReadAllTextAsync(commandPath);
        var plainPath = Directory.GetFiles(OutputPath, "PlainCommand.ts", SearchOption.AllDirectories).SingleOrDefault();
        _plainCommand = plainPath is null ? Output : await File.ReadAllTextAsync(plainPath);
    }

    [Fact] void should_save_success_state() => File.Exists(StatePath).ShouldBeTrue();
    [Fact] void should_not_report_load_errors() => Output.ShouldNotContain("Runtime validation discovery failed");
    [Fact] void should_succeed() => ExitCode.ShouldEqual(0);
    [Fact] void should_generate_a_command_without_a_validator() => _plainCommand.ShouldContain("export class PlainCommand");
    [Fact] void should_not_invent_a_validator() => _plainCommand.ShouldNotContain("readonly validation:");
    [Fact] void should_emit_the_validator() => _command.ShouldContain("export class CreateApplicationValidator");
    [Fact] void should_emit_the_rule() => _command.ShouldContain("this.ruleFor(c => c.name).notEmpty()");
    [Fact] void should_emit_the_validation_member() => _command.ShouldContain("readonly validation: CommandValidator = new CreateApplicationValidator()");
}
