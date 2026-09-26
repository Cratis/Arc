// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_command_with_severities : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    string _unattributedCode = null!;
    IReadOnlyList<string> _diagnostics = null!;

    void Establish() => _runtime = new JavaScriptRuntime();

    void Because()
    {
        var type = typeof(TestCommandWithSeverities);
        var descriptor = new CommandDescriptor(
            type,
            type.GetMethod(nameof(object.ToString))!,
            "/test",
            "TestCommandWithSeverities",
            type.GetProperties().Select(_ => _.ToPropertyDescriptor()).ToList(),
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            false,
            ModelDescriptor.Empty,
            [],
            null,
            ValidationRulesExtractor.ExtractValidationRules(type.Assembly, type),
            false,
            [])
        {
            BlockOnValidationSeverity = 2
        };
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(descriptor);
        _unattributedCode = InMemoryProxyGenerator.GenerateCommand(descriptor with { BlockOnValidationSeverity = null });
        _diagnostics = _runtime.GetSyntacticDiagnostics(_generatedCode);
    }

    [Fact] void should_emit_warning_severity() => _generatedCode.ShouldContain("this.ruleFor(c => c.warning).notEmpty().withSeverity(2)");
    [Fact] void should_emit_information_severity() => _generatedCode.ShouldContain("this.ruleFor(c => c.information).notEmpty().withSeverity(1)");
    [Fact] void should_emit_arc_extension_warning() => _generatedCode.ShouldContain("this.ruleFor(c => c.arcWarning).notEmpty().withSeverity(2)");
    [Fact] void should_not_emit_default_error_severity() => _generatedCode.ShouldContain("this.ruleFor(c => c.error).notEmpty();");
    [Fact] void should_not_require_new_client_api_for_default_error() => _generatedCode.ShouldNotContain("withSeverity(3)");
    [Fact] void should_defer_dynamic_severity_to_the_server() => _generatedCode.ShouldNotContain("this.ruleFor(c => c.dynamic)");
    [Fact] void should_defer_property_dependent_severity_to_the_server() => _generatedCode.ShouldNotContain("this.ruleFor(c => c.dynamicWithProperty)");
    [Fact] void should_emit_the_command_policy() => _generatedCode.ShouldContain("readonly blockOnValidationSeverity = 2");
    [Fact] void should_defer_dynamic_severity_even_without_a_policy() => _unattributedCode.ShouldNotContain("this.ruleFor(c => c.dynamic)");
    [Fact] void should_produce_valid_typescript() => _diagnostics.ShouldBeEmpty();

    public void Dispose() => _runtime?.Dispose();
}
