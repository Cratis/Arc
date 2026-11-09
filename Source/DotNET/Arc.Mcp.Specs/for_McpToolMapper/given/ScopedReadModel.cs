// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Mcp.for_McpToolMapper.given;

[RequiresScope("app:reports")]
public class ScopedReadModel
{
    public string Value { get; init; } = string.Empty;

    [RequiresScope("app:export")]
    public static IEnumerable<ScopedReadModel> Reports() => [];
}
