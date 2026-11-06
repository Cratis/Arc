// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_no_caller_is_verified : given.a_capability_gate
{
    Task Because() => Invoke();
    [Fact] void should_challenge() => _context.Response.StatusCode.ShouldEqual(401);
    [Fact] void should_advertise_bearer() => _context.Response.Headers.WWWAuthenticate.ToString().ShouldEqual("Bearer");
    [Fact] void should_not_run_handler() => _nextRan.ShouldBeFalse();
    [Fact] void should_write_problem_json() => _context.Response.ContentType.ShouldEqual("application/problem+json");
    [Fact]
    void should_write_only_problem_fields()
    {
        using var problem = JsonDocument.Parse(((MemoryStream)_context.Response.Body).ToArray());
        problem.RootElement.EnumerateObject().Select(_ => _.Name).ShouldEqual(["type", "title", "status"]);
        problem.RootElement.GetProperty("status").GetInt32().ShouldEqual(401);
    }
}
