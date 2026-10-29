// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroup;

public class when_resolving_the_group_of_an_untagged_query_method_on_a_tagged_type : Specification
{
    string? _result;

    void Because() => _result = EndpointGroup.For(typeof(InternalModel).GetMethod(nameof(InternalModel.Query))!, typeof(InternalModel));

    [Fact] void should_use_the_type_group() => _result.ShouldEqual("internal");

    [EndpointGroup("internal")]
    public record InternalModel
    {
        public static int Query() => 0;
    }
}
