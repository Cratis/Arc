// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AspNetCore;

/// <summary>
/// Represents a single message in a live feed.
/// </summary>
public record LiveFeedMessage(string Author, string Text, DateTimeOffset PostedAt);
