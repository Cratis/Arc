// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_EndpointGroupExtensions;

public class when_generating_for_the_default_group : Specification
{
    bool _includesPublic;
    bool _includesInternal;

    void Because()
    {
        _includesPublic = typeof(PublicCommand).IsInEndpointGroup(null);
        _includesInternal = typeof(InternalCommand).IsInEndpointGroup(null);
    }

    [Fact] void should_include_untagged_artifacts() => _includesPublic.ShouldBeTrue();
    [Fact] void should_skip_artifacts_in_a_named_group() => _includesInternal.ShouldBeFalse();
}
