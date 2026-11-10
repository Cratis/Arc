// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_only_an_oauth_allowlist_is_installed : a_pat_scope_validation
{
    void Establish() => _allowlist.Kind.Returns(CallerCredentialKind.OAuthAccessToken);

    void Because() => Validate();

    [Fact] void should_start_without_pat_configuration() => _error.ShouldBeNull();
    [Fact] void should_not_warn() => _logs.Entries.ShouldBeEmpty();
}
