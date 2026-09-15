// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// Same fix as <see cref="when_generating_single_instance_query_proxy"/>, but for the observable query
/// template — <see cref="InMemoryProxyGenerator.GenerateQuery"/> dispatches to
/// <see cref="TemplateTypes.ObservableQuery"/> when <see cref="QueryDescriptor.IsObservable"/> is set, and
/// that template carried the identical <c>{} as any</c> line.
/// </summary>
public class when_generating_single_instance_observable_query_proxy : Specification, IDisposable
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
            true,
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
