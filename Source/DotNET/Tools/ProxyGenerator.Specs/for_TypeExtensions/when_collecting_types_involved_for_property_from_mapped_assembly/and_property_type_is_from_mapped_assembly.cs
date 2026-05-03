// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_collecting_types_involved_for_property_from_mapped_assembly;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_property_type_is_from_mapped_assembly : Specification
{
    class MappedExternalType
    {
        public string Value { get; set; } = string.Empty;
    }

    IList<Type> _typesInvolved = null!;
    PropertyDescriptor _property = null!;

    void Establish()
    {
        var assemblyName = typeof(MappedExternalType).Assembly.GetName().Name!;
        TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string> { [assemblyName] = "@test/scene" });

        _typesInvolved = new List<Type>();
        _property = new PropertyDescriptor(
            typeof(MappedExternalType),
            "externalProperty",
            "MappedExternalType",
            "MappedExternalType",
            "@test/scene",
            false,
            false,
            false,
            null);
    }

    void Because() => _property.CollectTypesInvolved(_typesInvolved);

    void Destroy() => TypeExtensions.SetAssemblyPackageMappings(new Dictionary<string, string>());

    [Fact] void should_not_add_the_mapped_type_to_types_involved() => _typesInvolved.ShouldBeEmpty();
}