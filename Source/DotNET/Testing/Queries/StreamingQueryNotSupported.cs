// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// The exception that is thrown when a query scenario executes a streaming query instead of a snapshot query.
/// </summary>
/// <param name="methodName">The name of the streaming query.</param>
public sealed class StreamingQueryNotSupported(string methodName)
    : Exception($"Query scenario '{methodName}' supports snapshot queries only; streaming subscriptions require a hosted transport.");
