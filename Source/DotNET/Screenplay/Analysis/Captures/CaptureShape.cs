// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Captures;

/// <summary>
/// The parameters a capture reactor is a rendering of.
/// </summary>
/// <param name="TypeName">The name of the reactor type, which is also the name of the capture.</param>
/// <param name="Origin">The event store the captured event originates from.</param>
/// <param name="Identity">The identity of the capture, which also identifies the reactor.</param>
/// <param name="Handler">The name of the handler of the captured event.</param>
/// <param name="SourceType">The fully qualified name of the captured event.</param>
/// <param name="SourceProperties">The names of the properties of the captured event, in declaration order.</param>
/// <param name="Key">The name of the captured field the capture is keyed by.</param>
/// <param name="KeyIsEventSource">Whether the key must be the event source of the delivered event.</param>
/// <param name="AppendedType">The fully qualified name of the appended event.</param>
/// <param name="Mappings">The type and captured field each constructor argument of the appended event is read from.</param>
public record CaptureShape(
    string TypeName,
    string Origin,
    string Identity,
    string Handler,
    string SourceType,
    IReadOnlyList<string> SourceProperties,
    string Key,
    bool KeyIsEventSource,
    string AppendedType,
    IReadOnlyList<CaptureArgument> Mappings);
