// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public record ReplyToNote(string Text)
{
    public string Handle() => Text == "fail" ? throw new InvalidOperationException("Internal command failure.") : $"reply:{Text}";
}
