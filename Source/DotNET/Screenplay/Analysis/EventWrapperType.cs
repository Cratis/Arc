// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Holds the metadata name of the Chronicle wrapper that appends an event to an explicit destination.
/// </summary>
/// <remarks>
/// A mistyped name matches nothing and raises no error, so every reader shares this one constant.
/// </remarks>
internal static class EventWrapperType
{
    /// <summary>The fully qualified metadata name of <c>EventForEventSourceId</c>.</summary>
    internal const string MetadataName = "Cratis.Chronicle.EventSequences.EventForEventSourceId";
}
