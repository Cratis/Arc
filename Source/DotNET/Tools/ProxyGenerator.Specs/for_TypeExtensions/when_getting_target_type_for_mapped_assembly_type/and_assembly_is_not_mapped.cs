// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_target_type_for_mapped_assembly_type;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_assembly_is_not_mapped : Specification
{
    class TypeFromUnmappedAssembly
    {
        public string Name { get; set; } = string.Empty;
    }

    TargetType _result = null!;

    void Establish() => TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string>());

    void Because() => _result = typeof(TypeFromUnmappedAssembly).GetTargetType();

    [Fact] void should_have_the_type_name() => _result.Type.ShouldEqual("TypeFromUnmappedAssembly");
    [Fact] void should_not_have_a_module() => _result.Module.ShouldEqual(string.Empty);
    [Fact] void should_not_be_from_package() => _result.FromPackage.ShouldBeFalse();
}
