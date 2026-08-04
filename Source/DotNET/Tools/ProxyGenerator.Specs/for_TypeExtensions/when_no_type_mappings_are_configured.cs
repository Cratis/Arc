// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

/// <summary>
/// The guarantee that matters most for this seam. Generated proxies are committed in consumer repos, so
/// a build that configures no mappings has to generate exactly what it generated before - anything else
/// lands as a large silent diff in somebody else's repository.
/// </summary>
/// <remarks>
/// Asserts that configuring a mapping and clearing it leaves nothing behind, rather than restating the
/// built-in mappings themselves. The built-in map is what the rest of this suite already covers, and
/// pinning individual values here would cement whichever ones happen to be under discussion.
/// </remarks>
[Collection(TypeMappingCollection.Name)]
public class when_no_type_mappings_are_configured : given.no_type_mappings
{
    TargetType _beforeAnyMapping = null!;
    TargetType _afterClearingMappings = null!;
    TargetType _guid = null!;
    TargetType _point = null!;
    TargetType _unmapped = null!;

    void Establish()
    {
        _beforeAnyMapping = typeof(DateTime).GetTargetType();
        TypeExtensions.SetTypeMappings([(typeof(DateTime).FullName!, "Instant", "@acme/time")]);
    }

    void Because()
    {
        TypeExtensions.SetTypeMappings([]);
        _afterClearingMappings = typeof(DateTime).GetTargetType();
        _guid = typeof(Guid).GetTargetType();
        _point = typeof(Point).GetTargetType();
        _unmapped = typeof(Version).GetTargetType();
    }

    [Fact] void should_leave_nothing_behind_when_mappings_are_cleared() => _afterClearingMappings.ShouldEqual(_beforeAnyMapping);
    [Fact] void should_still_map_guid_from_the_fundamentals_package() => _guid.Module.ShouldEqual("@cratis/fundamentals");
    [Fact] void should_still_map_point_from_the_fundamentals_package() => _point.Module.ShouldEqual("@cratis/fundamentals");
    [Fact] void should_leave_an_unmapped_type_alone() => _unmapped.Type.ShouldEqual(nameof(Version));
    [Fact] void should_not_import_an_unmapped_type() => _unmapped.Module.ShouldBeEmpty();
}
