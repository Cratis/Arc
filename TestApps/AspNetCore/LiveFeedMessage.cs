// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AspNetCore;

/// <summary>
/// Represents a single message in a live feed.
/// </summary>
/// <param name="Author">The author of the message.</param>
/// <param name="Text">The content of the message.</param>
/// <param name="PostedAt">The timestamp when the message was posted.</param>
public record LiveFeedMessage(string Author, string Text, DateTimeOffset PostedAt);
