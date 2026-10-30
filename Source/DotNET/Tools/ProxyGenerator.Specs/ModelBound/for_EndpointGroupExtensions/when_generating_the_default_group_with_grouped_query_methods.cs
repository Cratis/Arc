// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_EndpointGroupExtensions;

public class when_generating_the_default_group_with_grouped_query_methods : for_MetadataQueryAttributes.given.a_metadata_discovery
{
    bool _includesTagged;
    bool _includesPublic;

    void Because()
    {
        var model = MetadataType<given.query_methods>();
        _includesTagged = model.GetMethod("Internal").IsInEndpointGroup(model, null);
        _includesPublic = model.GetMethod("Public").IsInEndpointGroup(model, null);
    }

    [Fact] void should_skip_the_tagged_query() => _includesTagged.ShouldBeFalse();
    [Fact] void should_include_the_untagged_sibling() => _includesPublic.ShouldBeTrue();
}
