// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroup;

public class when_resolving_the_group_of_a_tagged_query_method : Specification
{
    string? _onPublicType;
    string? _onGroupedType;

    void Because()
    {
        _onPublicType = EndpointGroup.For(typeof(PublicModel).GetMethod(nameof(PublicModel.Query))!, typeof(PublicModel));
        _onGroupedType = EndpointGroup.For(typeof(InternalModel).GetMethod(nameof(InternalModel.Query))!, typeof(InternalModel));
    }

    [Fact] void should_use_the_method_group_on_an_untagged_type() => _onPublicType.ShouldEqual("internal");
    [Fact] void should_override_the_type_group() => _onGroupedType.ShouldEqual("ops");

    public record PublicModel
    {
        [EndpointGroup("internal")]
        public static int Query() => 0;
    }

    [EndpointGroup("internal")]
    public record InternalModel
    {
        [EndpointGroup("ops")]
        public static int Query() => 0;
    }
}
