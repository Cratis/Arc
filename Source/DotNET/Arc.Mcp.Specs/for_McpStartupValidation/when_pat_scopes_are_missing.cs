// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_pat_scopes_are_missing : a_pat_scope_validation
{
    void Because() => Validate();

    [Fact] void should_only_warn_not_refuse_startup() => _error.ShouldBeNull();
    [Fact] void should_warn_for_each_missing_alternative_scope() => _logs.Entries.Select(_ => _.Values[0]).ShouldEqual(new object?[] { "audit:export", "audit:read" });
    [Fact] void should_name_the_issuance_setting() => _logs.Entries.All(_ => _.Message.Contains("PatOptions.AllowedScopes", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_mcp_scope_settings() => _logs.Entries.All(_ => _.Message.Contains("ReadScope/WriteScope", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_use_warning_level() => _logs.Entries.Select(_ => _.Level).Distinct().ShouldContainOnly(LogLevel.Warning);
}
