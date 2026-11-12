// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpStartupValidation.given;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_no_scope_allowlist_is_installed : a_pat_scope_validation
{
    void Establish() => _services.RemoveAll<ICredentialScopeAllowlist>();

    void Because() => Validate();

    [Fact] void should_start_without_pat_configuration() => _error.ShouldBeNull();
    [Fact] void should_not_warn() => _logs.Entries.ShouldBeEmpty();
}
