// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;

namespace Cratis.Arc.Mcp.for_McpToolCeiling;

public class when_a_name_is_duplicated : a_ceiling_context
{
    Exception? _controlError;

    void Establish() => _exposure
        .Expose<DeclaredCommand>("same_name", "First.")
        .Expose<UndeclaredCommand>("same_name", "Second.");

    void Because()
    {
        Build();
        _controlError = Catch.Exception(() => McpToolCeiling.Build(
            new McpExposure().Expose<DeclaredCommand>("first_name", "First.").Expose<UndeclaredCommand>("second_name", "Second."),
            _options,
            _commands,
            _queries));
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<McpToolNameDuplicated>();
    [Fact] void should_not_build_a_ceiling() => _ceiling.ShouldBeNull();
    [Fact] void should_accept_distinct_names() => _controlError.ShouldBeNull();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
