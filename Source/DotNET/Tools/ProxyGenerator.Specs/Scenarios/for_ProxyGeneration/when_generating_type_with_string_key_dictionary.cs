// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_type_with_string_key_dictionary : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    TypeDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var type = typeof(TypeWithStringKeyDictionaryProperty);
        var properties = type.GetProperties()
            .Select(p => p.ToPropertyDescriptor())
            .ToList();

        _descriptor = new TypeDescriptor(
            type,
            "TypeWithStringKeyDictionaryProperty",
            properties,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            []);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateType(_descriptor);

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
    [Fact] void should_contain_class_name() => _generatedCode.ShouldContain("TypeWithStringKeyDictionaryProperty");
    [Fact] void should_contain_record_type() => _generatedCode.ShouldContain("Record<string, ScenarioDictionaryValueType>");
    [Fact] void should_use_object_constructor() => _generatedCode.ShouldContain("@field(Object)");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
