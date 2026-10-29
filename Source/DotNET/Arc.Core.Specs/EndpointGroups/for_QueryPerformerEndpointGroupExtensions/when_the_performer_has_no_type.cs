// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.EndpointGroups.for_QueryPerformerEndpointGroupExtensions;

public class when_the_performer_has_no_type : Specification
{
    IQueryPerformer _performer;
    string? _result;

    void Establish()
    {
        _performer = Substitute.For<IQueryPerformer>();
        _performer.Type.Returns((Type)null!);
    }

    void Because() => _result = _performer.GetEndpointGroup();

    [Fact] void should_use_the_default_group() => _result.ShouldBeNull();
}
