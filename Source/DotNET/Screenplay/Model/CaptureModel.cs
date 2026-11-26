// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a stateful once-only capture of foreign events, recovered from its reactor.
/// </summary>
/// <param name="Name">The name of the capture.</param>
/// <param name="Key">The name of the captured field that identifies what is remembered.</param>
/// <param name="KeyIsEventSource">Whether the reactor asserts that the key is the event source of the delivered event.</param>
/// <param name="SourceEvents">The names of the foreign events the capture reads.</param>
/// <param name="AppendedEvent">The name of the event the capture appends.</param>
/// <param name="Mappings">The appended event properties and the captured fields they are filled from.</param>
public record CaptureModel(
    string Name,
    string Key,
    bool KeyIsEventSource,
    IEnumerable<string> SourceEvents,
    string AppendedEvent,
    IEnumerable<CaptureMappingModel> Mappings);
