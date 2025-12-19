// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_query_proxy : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    QueryDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var readModelType = typeof(SimpleReadModel);
        var queryMethod = readModelType.GetMethod("GetAll");

        _descriptor = new QueryDescriptor(
            readModelType,
            queryMethod,
            "/api/queries/simple-read-model/get-all",
            "GetAll",
            "SimpleReadModel",
            "SimpleReadModel",
            true,
            false,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            [],
            [],
            [readModelType],
            null);
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

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_contain_class_name() => _generatedCode.ShouldContain("class GetAll");
    [Fact] void should_contain_route() => _generatedCode.ShouldContain("/api/queries/simple-read-model/get-all");
    [Fact] void should_contain_model_type() => _generatedCode.ShouldContain("SimpleReadModel");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
