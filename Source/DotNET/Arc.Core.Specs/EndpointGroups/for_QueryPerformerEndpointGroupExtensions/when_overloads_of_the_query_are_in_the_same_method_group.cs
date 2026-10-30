// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.EndpointGroups.for_QueryPerformerEndpointGroupExtensions;

public class when_overloads_of_the_query_are_in_the_same_method_group : Specification
{
    IQueryPerformer _performer;
    string? _group;

    void Establish()
    {
        _performer = Substitute.For<IQueryPerformer>();
        _performer.Type.Returns(typeof(Model));
        _performer.Name.Returns(new QueryName(nameof(Model.Query)));
        _performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Model.Query"));
    }

    void Because() => _group = _performer.GetEndpointGroup();

    [Fact] void should_use_the_method_group() => _group.ShouldEqual("internal");

    public record Model
    {
        [EndpointGroup("internal")]
        public static int Query() => 0;

        [EndpointGroup("internal")]
        public static int Query(int id) => id;
    }
}
