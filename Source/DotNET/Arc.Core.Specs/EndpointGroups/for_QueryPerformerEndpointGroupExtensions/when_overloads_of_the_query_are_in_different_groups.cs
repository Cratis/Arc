// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.EndpointGroups.for_QueryPerformerEndpointGroupExtensions;

public class when_overloads_of_the_query_are_in_different_groups : Specification
{
    IQueryPerformer _performer;
    Exception? _error;

    void Establish()
    {
        _performer = Substitute.For<IQueryPerformer>();
        _performer.Type.Returns(typeof(Model));
        _performer.Name.Returns(new QueryName(nameof(Model.Query)));
        _performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Model.Query"));
    }

    void Because() => _error = Catch.Exception(() => _performer.GetEndpointGroup());

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<AmbiguousQueryEndpointGroup>();

    public record Model
    {
        public static int Query() => 0;

        [EndpointGroup("internal")]
        public static int Query(int id) => id;
    }
}
