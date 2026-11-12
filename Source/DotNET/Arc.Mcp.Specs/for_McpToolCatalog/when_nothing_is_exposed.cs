// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_nothing_is_exposed : a_catalog_context
{
    void Because() => Build();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_be_empty() => _catalog.IsEmpty.ShouldBeTrue();
    [Fact] void should_list_no_tools() => _catalog.Tools.ShouldBeEmpty();
    [Fact] void should_not_reach_an_existing_command_by_its_clr_name() => _catalog.TryGet(nameof(DeclaredCommand), out _).ShouldBeFalse();
    [Fact] void should_not_reach_a_null_name() => _catalog.TryGet(null, out _).ShouldBeFalse();
}
