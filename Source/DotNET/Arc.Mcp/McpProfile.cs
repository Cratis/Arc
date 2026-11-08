// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Defines which credential families the MCP endpoint accepts.
/// </summary>
public enum McpProfile
{
    /// <summary>Only personal access tokens are accepted. No protected resource metadata is advertised.</summary>
    PatOnly = 0,

    /// <summary>Only OAuth access tokens are accepted.</summary>
    OAuthOnly = 1,

    /// <summary>Personal access tokens and OAuth access tokens are both accepted, each by its own verifier.</summary>
    Combined = 2
}
