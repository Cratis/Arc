// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_pat_scopes_are_allowed : a_pat_scope_validation
{
    void Establish() => _allowlist.Scopes.Returns(new HashSet<string> { "app:read", "audit:read", "audit:export" });

    void Because() => Validate();

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_not_warn() => _logs.Entries.ShouldBeEmpty();
}
