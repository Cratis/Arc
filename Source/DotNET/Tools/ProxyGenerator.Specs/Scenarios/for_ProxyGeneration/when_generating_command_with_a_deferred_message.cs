// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// What the generated artifact must not contain. A frozen message would be a harmless staleness were the generated
/// validator advisory, but it is not: a failing client rule short-circuits the request, so the build machine's
/// answer is the one the user is shown and the server that would have resolved it correctly is never asked.
/// </summary>
public class when_generating_command_with_a_deferred_message : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    CommandDescriptor _descriptor = null!;
    IReadOnlyList<string> _diagnostics = null!;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var commandType = typeof(TypeWithDeferredMessage);
        var method = commandType.GetMethod("Handle") ?? typeof(object).GetMethod("GetHashCode");
        var properties = commandType.GetProperties().Select(_ => _.ToPropertyDescriptor()).ToList();

        _descriptor = new CommandDescriptor(
            commandType,
            method,
            "/api/commands/deferred-message",
            "TypeWithDeferredMessage",
            properties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            false,
            ModelDescriptor.Empty,
            [],
            null,
            ValidationRulesExtractor.ExtractValidationRules(commandType.Assembly, commandType),
            false,
            []);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_descriptor);
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
