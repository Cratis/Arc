// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_from_mapped_assembly;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_assembly_is_mapped : Specification
{
    class TypeFromMappedAssembly
    {
        public string Name { get; set; } = string.Empty;
    }

    bool _result;

    void Establish()
    {
        var assemblyName = typeof(TypeFromMappedAssembly).Assembly.GetName().Name!;
        TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string> { [assemblyName] = "@test/scene" });
    }

    void Because() => _result = typeof(TypeFromMappedAssembly).IsFromMappedAssembly();

    void Destroy() => TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string>());

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
