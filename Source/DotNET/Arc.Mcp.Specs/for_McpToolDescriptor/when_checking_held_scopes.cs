// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpToolDescriptor;

public class when_checking_held_scopes : Specification
{
    McpToolDescriptor _tool;
    McpToolDescriptor _emptyAlternative;
    McpToolDescriptor _emptyRequired;
    McpToolDescriptor _blankAlternative;

    void Establish()
    {
        _tool = McpToolDescriptor.Custom("tool", "A tool.", ["a", "b"], McpToolAccess.Read) with { AlternativeScopes = [["c", "d"], ["e"]] };
        _emptyAlternative = _tool with { AlternativeScopes = [[]] };
        _emptyRequired = _tool with { RequiredScopes = [], AlternativeScopes = [] };
        _blankAlternative = _tool with { AlternativeScopes = [[" "]] };
    }

    [Fact] void should_be_satisfied_by_the_required_scopes() => _tool.IsSatisfiedBy(Held("a", "b")).ShouldBeTrue();
    [Fact] void should_be_satisfied_by_a_complete_alternative() => _tool.IsSatisfiedBy(Held("c", "d")).ShouldBeTrue();
    [Fact] void should_be_satisfied_by_a_single_scope_alternative() => _tool.IsSatisfiedBy(Held("e")).ShouldBeTrue();
    [Fact] void should_not_be_satisfied_by_part_of_the_required_scopes() => _tool.IsSatisfiedBy(Held("a")).ShouldBeFalse();
    [Fact] void should_not_be_satisfied_by_parts_of_different_sets() => _tool.IsSatisfiedBy(Held("a", "c")).ShouldBeFalse();
    [Fact] void should_not_be_satisfied_by_nothing() => _tool.IsSatisfiedBy(Held()).ShouldBeFalse();
    [Fact] void should_not_let_an_empty_alternative_grant() => _emptyAlternative.IsSatisfiedBy(Held()).ShouldBeFalse();
    [Fact] void should_not_let_empty_required_scopes_grant() => _emptyRequired.IsSatisfiedBy(Held()).ShouldBeFalse();
    [Fact] void should_not_let_a_blank_alternative_grant() => _blankAlternative.IsSatisfiedBy(Held(" ")).ShouldBeFalse();

    static HashSet<string> Held(params string[] scopes) => [.. scopes];
}
