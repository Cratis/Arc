// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception thrown when a source-only pin is given for a per-stream model.
/// </summary>
/// <param name="readModelType">The per-stream model.</param>
public class PinnedPerStreamReadModelIsNotSupported(Type readModelType) : Exception(
    $"'{readModelType.Name}' is a per-stream read model. A source-only pin cannot identify its route. " +
    "Seed routed events with Given.ForEventSource(...).OnRoute(...).Events(...) instead.");
