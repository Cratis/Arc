// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public class PersonDirectory : ICallerPersonDirectory
{
    public Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken) =>
        Task.FromResult(CallerPersonFound.Found(new("person")));
}
