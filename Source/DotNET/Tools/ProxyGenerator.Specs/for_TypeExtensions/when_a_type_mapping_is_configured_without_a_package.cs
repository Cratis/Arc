// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

[Collection(TypeMappingCollection.Name)]
public class when_a_type_mapping_is_configured_without_a_package : given.no_type_mappings
{
    TargetType _result = null!;

    void Establish() => TypeExtensions.SetTypeMappings([(typeof(Version).FullName!, "string", string.Empty)]);

    void Because() => _result = typeof(Version).GetTargetType();

    [Fact] void should_generate_the_declared_type() => _result.Type.ShouldEqual("string");
    [Fact] void should_not_import_anything() => _result.Module.ShouldBeEmpty();
    [Fact] void should_not_be_from_a_package() => _result.FromPackage.ShouldBeFalse();
}
