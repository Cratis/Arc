// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_EndpointGroupExtensions;

public class when_generating_for_a_selected_group : Specification
{
    bool _includesPublic;
    bool _includesInternal;

    void Because()
    {
        _includesPublic = typeof(PublicCommand).IsInEndpointGroup("internal");
        _includesInternal = typeof(InternalCommand).IsInEndpointGroup("internal");
    }

    [Fact] void should_skip_untagged_artifacts() => _includesPublic.ShouldBeFalse();
    [Fact] void should_include_artifacts_in_the_group() => _includesInternal.ShouldBeTrue();
}
