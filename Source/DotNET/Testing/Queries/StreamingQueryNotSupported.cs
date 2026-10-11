// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// The exception that is thrown when a query scenario executes an asynchronous stream through Perform instead of Observe.
/// </summary>
/// <param name="methodName">The name of the streaming query.</param>
public sealed class StreamingQueryNotSupported(string methodName)
    : Exception($"Query scenario '{methodName}' returns an asynchronous stream; use QueryScenario.Observe to read its emissions.");
