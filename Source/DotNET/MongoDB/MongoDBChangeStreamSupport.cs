// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Identifies server responses that report unavailable change streams, not transient watch failures.
/// </summary>
internal static class MongoDBChangeStreamSupport
{
    /// <summary>
    /// Determines whether a watch failure reports missing change-stream support.
    /// </summary>
    /// <param name="error">The driver's command failure.</param>
    /// <returns>Whether the server cannot provide change streams.</returns>
    internal static bool IsMissing(MongoCommandException error)
    {
        if (error.Code == 40573)
        {
            return true;
        }

        if (!error.Message.Contains("$changeStream", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return error.Code is 40324 or 16436 or 115 ||
            error.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("unrecognized pipeline stage", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("only supported on replica sets", StringComparison.OrdinalIgnoreCase);
    }
}
