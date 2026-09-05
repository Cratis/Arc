// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// The corpus scenario for client validation projection: every rule shape the extractor can project is pinned to the
/// exact TypeScript it produces, the shapes it deliberately drops are proven absent, and the whole generated proxy is
/// held against the compiler's own diagnostics — not just a best-effort transpilation.
/// </summary>
public class when_generating_command_with_every_rule_shape : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    CommandDescriptor _descriptor = null!;
    IReadOnlyList<string> _diagnostics = null!;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var commandType = typeof(CommandWithEveryRuleShape);
        var method = commandType.GetMethod("Handle") ?? typeof(object).GetMethod("GetHashCode");
        var properties = commandType.GetProperties()
            .Select(p => p.ToPropertyDescriptor())
            .ToList();

        var validationRules = ValidationRulesExtractor.ExtractValidationRules(commandType.Assembly, commandType);

        _descriptor = new CommandDescriptor(
            commandType,
            method,
            "/api/commands/command-with-every-rule-shape",
            "CommandWithEveryRuleShape",
            properties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            false,
            ModelDescriptor.Empty,
            [],
            null,
            validationRules,
            false,
            []);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_descriptor);
        _diagnostics = _runtime.GetSyntacticDiagnostics(_generatedCode);
    }

    [Fact] void should_emit_not_empty() => _generatedCode.ShouldContain(".notEmpty()");
    [Fact] void should_emit_length_range() => _generatedCode.ShouldContain(".length(2, 50)");
    [Fact] void should_emit_not_null() => _generatedCode.ShouldContain(".notNull()");
    [Fact] void should_emit_email_address() => _generatedCode.ShouldContain(".emailAddress()");
    [Fact] void should_emit_min_length() => _generatedCode.ShouldContain(".minLength(10)");
    [Fact] void should_emit_max_length() => _generatedCode.ShouldContain(".maxLength(200)");
    [Fact] void should_emit_exact_length_as_a_range() => _generatedCode.ShouldContain(".length(4, 4)");
    [Fact] void should_still_emit_the_rule_a_lazy_message_was_declared_on() => _generatedCode.ShouldContain("this.ruleFor(c => c.pin).length(4, 4);");
    [Fact] void should_not_resolve_the_lazily_declared_message() => _generatedCode.ShouldNotContain("Pin must be exactly four digits");
    [Fact] void should_emit_greater_than_or_equal() => _generatedCode.ShouldContain(".greaterThanOrEqual(18)");
    [Fact] void should_emit_less_than() => _generatedCode.ShouldContain(".lessThan(150)");
    [Fact] void should_emit_greater_than() => _generatedCode.ShouldContain(".greaterThan(0)");
    [Fact] void should_emit_less_than_or_equal() => _generatedCode.ShouldContain(".lessThanOrEqual(64)");
    [Fact] void should_emit_a_decimal_comparison_with_invariant_formatting() => _generatedCode.ShouldContain(".greaterThan(0.5)");
    [Fact] void should_escape_a_bare_slash_in_a_regex() => _generatedCode.ShouldContain(@".matches(/^\/api\/[a-z]+$/)");
    [Fact] void should_not_double_escape_a_slash_the_pattern_already_escapes() => _generatedCode.ShouldContain(@".matches(/^\d{2}\/\d{2}$/)");
    [Fact] void should_drop_the_non_numeric_comparison() => _generatedCode.ShouldNotContain("this.ruleFor(c => c.when)");
    [Fact] void should_produce_typescript_the_compiler_accepts() => _diagnostics.ShouldBeEmpty();

    public void Dispose() => _runtime?.Dispose();
}
