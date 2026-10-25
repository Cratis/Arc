// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroup;

public class when_resolving_the_group_of_an_untagged_type : Specification
{
    string? _result;

    void Because() => _result = EndpointGroup.For(typeof(PublicCommand));

    [Fact] void should_return_the_default_group() => _result.ShouldBeNull();

    public record PublicCommand;
}
