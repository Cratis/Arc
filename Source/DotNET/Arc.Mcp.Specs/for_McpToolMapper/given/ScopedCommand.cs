// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Mcp.for_McpToolMapper.given;

[RequiresScope("app:billing")]
[RequiresScope("app:write")]
public record ScopedCommand(string Value)
{
    [RequiresScope("app:audit")]
    public void Handle()
    {
    }
}
