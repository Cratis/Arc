// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace AspNetCore;

/// <summary>
/// Represents a command to post a message to the live feed.
/// </summary>
/// <remarks>
/// Demonstrates a model-bound command whose side effect is observed in real time through the
/// centralised SSE hub (<c>/.cratis/queries/sse</c>) via the <see cref="LiveFeed"/> read model.
/// </remarks>
[Command]
public record PostToFeed(string Author, string Text)
{
    /// <summary>
    /// Handles the command by appending the message to the shared live feed.
    /// </summary>
    /// <returns>The posted message.</returns>
    public LiveFeedMessage Handle()
    {
        LiveFeed.Post(Author, Text);
        return new LiveFeedMessage(Author, Text, DateTimeOffset.UtcNow);
    }
}
