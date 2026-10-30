// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_EndpointGroupExtensions;

public class when_generating_for_a_selected_group_with_grouped_query_methods : for_MetadataQueryAttributes.given.a_metadata_discovery
{
    bool _includesTagged;
    bool _includesPublic;
    bool _includesInherited;
    bool _includesOverridden;

    void Because()
    {
        var model = MetadataType<given.query_methods>();
        var grouped = MetadataType<given.grouped_query_methods>();
        _includesTagged = model.GetMethod("Internal").IsInEndpointGroup(model, "internal");
        _includesPublic = model.GetMethod("Public").IsInEndpointGroup(model, "internal");
        _includesInherited = grouped.GetMethod("Inherited").IsInEndpointGroup(grouped, "internal");
        _includesOverridden = grouped.GetMethod("Overridden").IsInEndpointGroup(grouped, "internal");
    }

    [Fact] void should_include_the_tagged_query() => _includesTagged.ShouldBeTrue();
    [Fact] void should_skip_the_untagged_sibling() => _includesPublic.ShouldBeFalse();
    [Fact] void should_inherit_the_read_model_group() => _includesInherited.ShouldBeTrue();
    [Fact] void should_prefer_the_query_group_over_the_read_model_group() => _includesOverridden.ShouldBeFalse();
}
