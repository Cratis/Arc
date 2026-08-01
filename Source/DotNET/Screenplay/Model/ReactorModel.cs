// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents something that observes events and causes an effect.
/// </summary>
/// <param name="Name">The name of the reactor.</param>
/// <param name="ObservedEvents">The names of the events the reactor observes.</param>
/// <param name="IsTranslating">Whether the reactor turns events into further events or commands.</param>
/// <param name="SourceFilePath">The path of the file implementing the reactor, if it is known.</param>
public record ReactorModel(
    string Name,
    IEnumerable<string> ObservedEvents,
    bool IsTranslating,
    string? SourceFilePath);
