// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Diagnostic descriptors for Arc Chronicle analyzers.
/// </summary>
static class DiagnosticDescriptors
{
    const string Category = "Arc.Chronicle";

    /// <summary>
    /// ARC005: Incorrect AggregateRoot event handler signature.
    /// </summary>
    public static readonly DiagnosticDescriptor ARC005_IncorrectAggregateRootEventHandlerSignature = new(
        id: "ARC005",
        title: "Incorrect AggregateRoot event handler signature",
        messageFormat: "Event handler method '{0}' on AggregateRoot '{1}' must have one of these signatures: void On(TEvent), Task On(TEvent), void On(TEvent, EventContext), or Task On(TEvent, EventContext). Found: {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Event handler methods (typically named 'On') on AggregateRoot types must accept an event parameter and optionally an EventContext parameter, and return void or Task.");
}
