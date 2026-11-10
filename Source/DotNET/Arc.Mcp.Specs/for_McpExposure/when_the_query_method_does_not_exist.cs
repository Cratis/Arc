// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpExposure;

public class when_the_query_method_does_not_exist : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => new McpExposure().ExposeQuery<ReadModelWithQueries>("Missing", "missing", "Missing."));

    [Fact] void should_fail_at_the_call_site() => _error.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_name_the_read_model_and_the_method() => (_error!.Message.Contains(nameof(ReadModelWithQueries)) && _error.Message.Contains("Missing")).ShouldBeTrue();
}
