// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_command_with_validation : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    CommandDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var commandType = typeof(CommandWithValidation);
        var method = commandType.GetMethod("Handle") ?? typeof(object).GetMethod("GetHashCode")!;
        var properties = commandType.GetProperties()
            .Select(p => p.ToPropertyDescriptor())
            .ToList();

        var validationRules = ValidationRulesExtractor.ExtractValidationRules(commandType.Assembly, commandType);

        _descriptor = new CommandDescriptor(
            commandType,
            method,
            "/api/commands/command-with-validation",
            "CommandWithValidation",
            properties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            false,
            ModelDescriptor.Empty,
            [],
            null,
            validationRules);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_descriptor);

        try
        {
            var transpiledCode = _runtime.TranspileTypeScript(_generatedCode);
            _typeScriptIsValid = !string.IsNullOrEmpty(transpiledCode);
        }
        catch
        {
            _typeScriptIsValid = false;
        }
    }

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_contain_validator_class() => _generatedCode.ShouldContain("class CommandWithValidationValidator");
    [Fact] void should_extend_command_validator() => _generatedCode.ShouldContain("extends CommandValidator");
    [Fact] void should_contain_rule_for_email() => _generatedCode.ShouldContain("this.ruleFor(c => c.email)");
    [Fact] void should_contain_not_empty_rule_for_email() => _generatedCode.ShouldContain(".notEmpty()");
    [Fact] void should_contain_email_address_rule() => _generatedCode.ShouldContain(".emailAddress()");
    [Fact] void should_contain_custom_message_for_email() => _generatedCode.ShouldContain(".withMessage('Email is required')");
    [Fact] void should_contain_rule_for_age() => _generatedCode.ShouldContain("this.ruleFor(c => c.age)");
    [Fact] void should_contain_greater_than_or_equal_rule() => _generatedCode.ShouldContain(".greaterThanOrEqual(18)");
    [Fact] void should_contain_min_length_rule_for_name() => _generatedCode.ShouldContain(".minLength(2)");
    [Fact] void should_contain_max_length_rule_for_name() => _generatedCode.ShouldContain(".maxLength(50)");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose() => _runtime?.Dispose();
}
