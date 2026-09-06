// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// Pins the fix for a single-instance query's <c>defaultValue</c>: the template used to emit
/// <c>{} as any</c> regardless of the model type, leaving an unnecessary <c>any</c> in otherwise fully
/// typed generated output.
/// </summary>
public class when_generating_single_instance_query_proxy : Specification, IDisposable
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
            "/api/queries/simple-read-model/get-by-id",
            "GetById",
            "SimpleReadModel",
            "SimpleReadModel",
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

    [Fact] void should_type_the_default_value_as_the_model() => _generatedCode.ShouldContain("readonly defaultValue: SimpleReadModel = {} as SimpleReadModel;");
    [Fact] void should_not_contain_as_any() => _generatedCode.ShouldNotContain("as any");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
