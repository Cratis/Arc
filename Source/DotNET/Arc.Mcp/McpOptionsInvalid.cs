// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when MCP options are missing or invalid for the configured profile.
/// </summary>
/// <param name="option">The option that is missing or invalid.</param>
/// <param name="profile">The configured profile.</param>
public class McpOptionsInvalid(string option, McpProfile profile)
    : InvalidMcpConfiguration($"McpOptions.{option} is missing or invalid for the {profile} profile.");
