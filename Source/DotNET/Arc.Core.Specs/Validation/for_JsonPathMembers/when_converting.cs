// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_JsonPathMembers;

public class when_converting : Specification
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("$")]
    void should_omit_a_root_member(string? path) => JsonPathMembers.From(path).ShouldBeEmpty();

    [Theory]
    [InlineData("$.from", "from")]
    [InlineData("$.items[2].when", "items[2].when")]
    [InlineData("$['Items'][2]['When']", "items[2].when")]
    [InlineData("$.From", "from")]
    void should_normalize_a_property_path(string path, string member) => JsonPathMembers.From(path).ShouldContainOnly(member);
}
