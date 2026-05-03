// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_target_type_for_mapped_assembly_type;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_assembly_is_mapped_to_package : Specification
{
    class TypeFromMappedAssembly
    {
        public string Name { get; set; } = string.Empty;
    }

    TargetType _result = null!;

    void Establish()
    {
        var assemblyName = typeof(TypeFromMappedAssembly).Assembly.GetName().Name!;
        TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string> { [assemblyName] = "@test/scene" });
    }

    void Because() => _result = typeof(TypeFromMappedAssembly).GetTargetType();

    void Destroy() => TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string>());

    [Fact] void should_have_the_type_name() => _result.Type.ShouldEqual("TypeFromMappedAssembly");
    [Fact] void should_have_the_package_as_module() => _result.Module.ShouldEqual("@test/scene");
    [Fact] void should_be_from_package() => _result.FromPackage.ShouldBeTrue();
}
