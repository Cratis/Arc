// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpOperations;

public class when_a_hosted_query_scope_is_released : Specification
{
    IServiceScope _scope;
    QueryResult _result;

    void Establish()
    {
        _scope = Substitute.For<IServiceScope>();
        _result = new QueryResult { OwnedScope = _scope };
    }

    void Because()
    {
        _result.ReleaseScope();
        _result.ReleaseScope();
    }

    [Fact] void should_dispose_the_scope_once() => _scope.Received(1).Dispose();
    [Fact] void should_read_a_typed_response_without_its_generic_type() => new CommandResult<string> { Response = "response" }.ResponsePayload.ShouldEqual("response");
    [Fact] void should_read_no_response_from_an_untyped_result() => new CommandResult().ResponsePayload.ShouldBeNull();
}
