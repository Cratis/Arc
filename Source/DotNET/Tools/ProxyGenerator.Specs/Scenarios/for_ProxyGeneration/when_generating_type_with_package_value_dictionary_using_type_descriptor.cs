// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// A dictionary whose value is a package-mapped type (here Guid) must emit that type's import. Without it the
/// generated `Record&lt;string, Guid&gt;` references an undeclared `Guid` and the consuming project fails to compile.
/// </summary>
public class when_generating_type_with_package_value_dictionary_using_type_descriptor : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    TypeDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();
        _descriptor = typeof(TypeWithStringKeyPackageValueDictionaryProperty).ToTypeDescriptor(string.Empty, 0);
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
    [Fact] void should_import_guid_from_fundamentals() => _generatedCode.ShouldContain("import { Guid } from '@cratis/fundamentals'");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
