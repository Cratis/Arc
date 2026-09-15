// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// Pins that the cast is emitted verbatim, not HTML-escaped, for a dictionary-backed model. The fix must use
/// the triple-stache <c>{{{Model}}}</c> — Handlebars HTML-escapes the double-stache form, which would turn
/// <c>Record&lt;string, number&gt;</c> into <c>Record&amp;lt;string, number&amp;gt;</c>, breaking the emitted
/// TypeScript. The type annotation on the same line already uses triple-stache for exactly this reason.
/// </summary>
public class when_generating_single_instance_query_proxy_for_a_dictionary_model : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    QueryDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var readModelType = typeof(SimpleReadModel);
        var queryMethod = readModelType.GetMethod("GetById");

        _descriptor = new QueryDescriptor(
            readModelType,
            queryMethod,
            "/api/queries/simple-read-model/get-counts",
            "GetCounts",
            "Record<string, number>",
            "Object",
            false,
            false,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            [],
            [],
            [readModelType],
            null,
            [],
            false,
            []);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateQuery(_descriptor);

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

    [Fact] void should_type_the_default_value_as_the_dictionary() => _generatedCode.ShouldContain("readonly defaultValue: Record<string, number> = {} as Record<string, number>;");
    [Fact] void should_not_html_escape_the_cast() => _generatedCode.ShouldNotContain("&lt;");
    [Fact] void should_not_contain_as_any() => _generatedCode.ShouldNotContain("as any");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
