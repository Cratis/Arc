// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// The query half of <see cref="when_generating_command_with_a_deferred_message"/>. One extractor feeds four
/// generation paths - two for commands, two for queries - and the generated query validator short-circuits its
/// request exactly as the command one does. A fix that covers only commands would leave this half emitting frozen
/// literals while looking fixed from a command-only spec, which is the specific way this defect hides.
/// </summary>
public class when_generating_query_with_a_deferred_message : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    QueryDescriptor _descriptor = null!;
    IReadOnlyList<string> _diagnostics = null!;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var queryType = typeof(TypeWithDeferredMessage);
        var method = queryType.GetMethod("Handle") ?? typeof(object).GetMethod("GetHashCode");
        var properties = queryType.GetProperties().Select(_ => _.ToPropertyDescriptor()).ToList();

        _descriptor = new QueryDescriptor(
            queryType,
            method,
            "/api/queries/deferred-message",
            "TypeWithDeferredMessage",
            nameof(String),
            "() => ({})",
            false,
            false,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            [],
            properties,
            [],
            null,
            ValidationRulesExtractor.ExtractValidationRules(queryType.Assembly, queryType),
            false,
            []);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateQuery(_descriptor);
        _diagnostics = _runtime.GetSyntacticDiagnostics(_generatedCode);
    }

    [Fact] void should_still_mirror_the_rule_client_side() => _generatedCode.ShouldContain("this.ruleFor(c => c.deferredMessaged).notEmpty()");
    [Fact] void should_not_bake_in_the_build_machines_answer() => _generatedCode.ShouldNotContain(AmbientMessages.NeutralCulture);
    [Fact] void should_keep_an_eagerly_declared_literal() => _generatedCode.ShouldContain($".withMessage('{TypeWithDeferredMessageValidator.EagerMessage}')");
    [Fact] void should_produce_typescript_the_compiler_accepts() => _diagnostics.ShouldBeEmpty();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
