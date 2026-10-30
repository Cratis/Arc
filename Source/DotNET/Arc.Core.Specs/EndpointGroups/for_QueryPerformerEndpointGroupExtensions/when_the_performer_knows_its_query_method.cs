// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.EndpointGroups.for_QueryPerformerEndpointGroupExtensions;

public class when_the_performer_knows_its_query_method : Specification
{
    IQueryPerformer _performer;
    string? _result;

    void Establish()
    {
        _performer = Substitute.For<IQueryPerformer, IAuthorizationQueryTarget>();
        _performer.Type.Returns(typeof(Model));
        ((IAuthorizationQueryTarget)_performer).AuthorizationMethod.Returns(typeof(Model).GetMethod(nameof(Model.Query)));
    }

    void Because() => _result = _performer.GetEndpointGroup();

    [Fact] void should_use_the_exact_method() => _result.ShouldEqual("internal");

    public record Model
    {
        [EndpointGroup("internal")]
        public static int Query() => 0;
    }
}
