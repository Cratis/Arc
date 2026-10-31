// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator;

public class when_generating_a_command_whose_validator_activation_needs_an_output_local_dependency : given.a_runtime_dependency_application
{
    string _command = string.Empty;

    async Task Because()
    {
        await Generate("RenameApplication");
        _command = CommandText("CreateApplication");
    }

    [Fact] void should_generate_successfully() => ExitCode.ShouldEqual(0);
    [Fact] void should_emit_the_validator() => _command.ShouldContain("export class CreateApplicationValidator extends CommandValidator<ICreateApplication>");
    [Fact] void should_emit_the_required_rule() => _command.ShouldContain("this.ruleFor(c => c.name).notEmpty()");
    [Fact] void should_emit_the_required_message() => _command.ShouldContain("Application name is required");
    [Fact] void should_emit_the_maximum_length() => _command.ShouldContain(".maxLength(64)");
    [Fact] void should_emit_exactly_two_rules() => (_command.Split("this.ruleFor(", StringSplitOptions.None).Length - 1).ShouldEqual(2);
    [Fact] void should_attach_validation() => _command.ShouldContain("readonly validation: CommandValidator = new CreateApplicationValidator();");
}
