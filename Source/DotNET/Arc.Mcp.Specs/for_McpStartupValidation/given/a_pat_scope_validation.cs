// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Cratis.Arc.Mcp.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp.for_McpStartupValidation.given;

public class a_pat_scope_validation : a_startup_validation
{
    private protected LogRecorder<McpStartupValidation> _logs;
    protected ICredentialScopeAllowlist _allowlist;

    void Establish()
    {
        _logs = new();
        _allowlist = Substitute.For<ICredentialScopeAllowlist>();
        _allowlist.Kind.Returns(CallerCredentialKind.PersonalAccessToken);
        _allowlist.Scopes.Returns(new HashSet<string> { "app:read" });
        _services.AddSingleton<ILogger<McpStartupValidation>>(_logs);
        _services.AddSingleton(_allowlist);
        _mcp = mcp => mcp.AddToolSource<AlternativeScopeTool>();
    }
}
