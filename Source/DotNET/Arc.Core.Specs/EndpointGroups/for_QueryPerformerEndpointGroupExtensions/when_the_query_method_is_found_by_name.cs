// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.EndpointGroups.for_QueryPerformerEndpointGroupExtensions;

public class when_the_query_method_is_found_by_name : Specification
{
    IQueryPerformer _performer;
    string? _result;

    void Establish()
    {
        _performer = Substitute.For<IQueryPerformer>();
        _performer.Type.Returns(typeof(Model));
        _performer.Name.Returns(new QueryName(nameof(Model.Query)));
    }

    void Because() => _result = _performer.GetEndpointGroup();

    [Fact] void should_use_the_named_method() => _result.ShouldEqual("internal");

    public record Model
    {
        [EndpointGroup("internal")]
        public static int Query() => 0;
    }
}
