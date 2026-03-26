// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using TestApps.Features.LiveFeed;

namespace AspNetCore.Features.LiveFeed;

/// <summary>
/// Provides an HTTP endpoint for posting to the live feed, demonstrating a command that
/// triggers real-time pushes to all SSE-subscribed clients.
/// </summary>
[Route("api/live-feed")]
public class LiveFeedController : ControllerBase
{
    /// <summary>
    /// Posts a new message to the live feed.
    /// </summary>
    /// <param name="command">The command payload.</param>
    /// <returns>The posted message.</returns>
    [HttpPost("post")]
    public LiveFeedMessage Post([FromBody] PostToFeed command) => command.Handle();
}
