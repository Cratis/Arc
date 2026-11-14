// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_observable_query_with_severities : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    string _withoutParametersCode = null!;
    IReadOnlyList<string> _diagnostics = null!;
    IReadOnlyList<string> _withoutParametersDiagnostics = null!;

    void Establish() => _runtime = new JavaScriptRuntime();

    void Because()
    {
        var type = typeof(TestCommandWithSeverities);
        var descriptor = new QueryDescriptor(
            type,
            type.GetMethod(nameof(object.ToString))!,
            "/test",
            "ObservableQueryWithSeverities",
            nameof(String),
            "() => ({})",
            false,
            true,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            type.GetProperties().Select(p => new RequestParameterDescriptor(p.PropertyType, p.Name, "string", "String", false, true)).ToList(),
            [],
            type.GetProperties().Select(_ => _.ToPropertyDescriptor()).ToList(),
            [],
            null,
            ValidationRulesExtractor.ExtractValidationRules(type.Assembly, type),
            false,
            []);
        _generatedCode = InMemoryProxyGenerator.GenerateQuery(descriptor);
        _withoutParametersCode = InMemoryProxyGenerator.GenerateQuery(descriptor with { Parameters = [] });
        _diagnostics = _runtime.GetSyntacticDiagnostics(_generatedCode);
        _withoutParametersDiagnostics = _runtime.GetSyntacticDiagnostics(_withoutParametersCode);
    }

    [Fact] void should_emit_warning_severity() => _generatedCode.ShouldContain("this.ruleFor(c => c.warning).notEmpty().withSeverity(2)");
    [Fact] void should_emit_information_severity() => _generatedCode.ShouldContain("this.ruleFor(c => c.information).notEmpty().withSeverity(1)");
    [Fact] void should_emit_arc_extension_warning() => _generatedCode.ShouldContain("this.ruleFor(c => c.arcWarning).notEmpty().withSeverity(2)");
    [Fact] void should_not_emit_default_error_severity() => _generatedCode.ShouldContain("this.ruleFor(c => c.error).notEmpty();");
    [Fact] void should_not_require_new_client_api_for_default_error() => _generatedCode.ShouldNotContain("withSeverity(3)");
    [Fact] void should_defer_dynamic_severity_to_the_server() => _generatedCode.ShouldNotContain("this.ruleFor(c => c.dynamic)");
    [Fact] void should_defer_property_dependent_severity_to_the_server() => _generatedCode.ShouldNotContain("this.ruleFor(c => c.dynamicWithProperty)");
    [Fact] void should_emit_the_validator_class() => _generatedCode.ShouldContain("class ObservableQueryWithSeveritiesValidator extends QueryValidator<ObservableQueryWithSeveritiesParameters>");
    [Fact] void should_assign_the_validator_to_the_query() => _generatedCode.ShouldContain("readonly validation: QueryValidator = new ObservableQueryWithSeveritiesValidator();");
    [Fact] void should_produce_valid_typescript() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_emit_warning_severity_without_parameters() => _withoutParametersCode.ShouldContain("this.ruleFor(c => c.warning).notEmpty().withSeverity(2)");
    [Fact] void should_emit_information_severity_without_parameters() => _withoutParametersCode.ShouldContain("this.ruleFor(c => c.information).notEmpty().withSeverity(1)");
    [Fact] void should_emit_arc_extension_warning_without_parameters() => _withoutParametersCode.ShouldContain("this.ruleFor(c => c.arcWarning).notEmpty().withSeverity(2)");
    [Fact] void should_keep_default_error_implicit_without_parameters() => _withoutParametersCode.ShouldContain("this.ruleFor(c => c.error).notEmpty();");
    [Fact] void should_not_emit_default_error_severity_without_parameters() => _withoutParametersCode.ShouldNotContain("withSeverity(3)");
    [Fact] void should_defer_dynamic_severity_without_parameters() => _withoutParametersCode.ShouldNotContain("this.ruleFor(c => c.dynamic)");
    [Fact] void should_defer_property_dependent_severity_without_parameters() => _withoutParametersCode.ShouldNotContain("this.ruleFor(c => c.dynamicWithProperty)");
    [Fact] void should_emit_the_validator_class_without_parameters() => _withoutParametersCode.ShouldContain("class ObservableQueryWithSeveritiesValidator extends QueryValidator<object>");
    [Fact] void should_assign_the_validator_without_parameters() => _withoutParametersCode.ShouldContain("readonly validation: QueryValidator = new ObservableQueryWithSeveritiesValidator();");
    [Fact] void should_produce_valid_typescript_without_parameters() => _withoutParametersDiagnostics.ShouldBeEmpty();

    public void Dispose() => _runtime?.Dispose();
}
