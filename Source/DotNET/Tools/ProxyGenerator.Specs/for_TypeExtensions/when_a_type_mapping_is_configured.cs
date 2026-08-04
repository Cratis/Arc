// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

[Collection(TypeMappingCollectionDefinition.Name)]
public class when_a_type_mapping_is_configured : given.no_type_mappings
{
    TargetType _result = null!;

    void Establish() => TypeExtensions.SetTypeMappings([(typeof(Version).FullName!, "SemanticVersion", "@acme/versions")]);

    void Because() => _result = typeof(Version).GetTargetType();

    [Fact] void should_generate_the_declared_type() => _result.Type.ShouldEqual("SemanticVersion");
    [Fact] void should_construct_with_the_declared_type() => _result.Constructor.ShouldEqual("SemanticVersion");
    [Fact] void should_import_from_the_declared_package() => _result.Module.ShouldEqual("@acme/versions");
    [Fact] void should_import_it_as_a_bare_specifier() => _result.FromPackage.ShouldBeTrue();
    [Fact] void should_keep_the_original_type() => _result.OriginalType.ShouldEqual(typeof(Version));
}
