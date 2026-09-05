// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

/// <summary>
/// The mapping is consulted ahead of the built-in map, which is what lets a consumer correct how an
/// existing type crosses the wire rather than only declare one the generator has never seen. Without
/// that ordering the seam could only ever add.
/// </summary>
[Collection(TypeMappingCollection.Name)]
public class when_a_type_mapping_overrides_a_built_in_one : given.no_type_mappings
{
    TargetType _before = null!;
    TargetType _after = null!;

    void Establish()
    {
        _before = typeof(DateTime).GetTargetType();
        TypeExtensions.SetTypeMappings([(typeof(DateTime).FullName!, "Instant", "@acme/time")]);
    }

    void Because() => _after = typeof(DateTime).GetTargetType();

    [Fact] void should_have_used_the_built_in_mapping_before() => _before.Type.ShouldEqual("Date");
    [Fact] void should_take_the_place_of_the_built_in_mapping() => _after.Type.ShouldEqual("Instant");
    [Fact] void should_import_from_the_declared_package() => _after.Module.ShouldEqual("@acme/time");
}
