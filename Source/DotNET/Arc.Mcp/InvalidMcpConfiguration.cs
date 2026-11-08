// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown at startup when MCP serving is configured in a way that would not fail closed.
/// </summary>
/// <param name="message">The error message.</param>
public class InvalidMcpConfiguration(string message) : Exception(message);
