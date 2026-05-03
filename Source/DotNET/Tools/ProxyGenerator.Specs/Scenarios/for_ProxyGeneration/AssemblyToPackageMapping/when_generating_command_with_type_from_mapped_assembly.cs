// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration.AssemblyToPackageMapping;

/// <summary>
/// An external type that is treated as coming from a mapped assembly.
/// </summary>
public class ExternalElement
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// A command that uses a type from a mapped assembly.
/// </summary>
[Command]
public class UpdateExternalElements
{
    /// <summary>
    /// Gets or sets the elements from an external package.
    /// </summary>
    public IEnumerable<ExternalElement> Elements { get; set; } = [];

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle() { }
}

[Collection(Cratis.Arc.ProxyGenerator.for_TypeExtensions.AssemblyPackageMappingCollectionDefinition.Name)]
public class when_generating_command_with_type_from_mapped_assembly : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    CommandDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var externalAssemblyName = typeof(ExternalElement).Assembly.GetName().Name!;
        TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string>
        {
            [externalAssemblyName] = "@test/external"
        });

        var commandType = typeof(UpdateExternalElements).GetTypeInfo();
        _descriptor = commandType.ToCommandDescriptor(
            targetPath: string.Empty,
            segmentsToSkip: 0,
            skipCommandNameInRoute: false,
            apiPrefix: "api",
            [commandType]);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_descriptor);

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

    void Destroy() => TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string>());

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_import_external_element_from_mapped_package() => _generatedCode.ShouldContain("@test/external");
    [Fact] void should_not_generate_relative_import_for_external_element() => _generatedCode.ShouldNotContain("./ExternalElement");
    [Fact] void should_contain_elements_property() => _generatedCode.ShouldContain("elements");

    public void Dispose()
    {
        _runtime?.Dispose();
        GC.SuppressFinalize(this);
    }
}
