// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_from_mapped_assembly;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_assembly_is_not_mapped : Specification
{
    class TypeFromUnmappedAssembly
    {
        public string Name { get; set; } = string.Empty;
    }

    bool _result;

    void Establish() => TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string>());

    void Because() => _result = typeof(TypeFromUnmappedAssembly).IsFromMappedAssembly();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
