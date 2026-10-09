// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a template part is blank or resolves to a sentinel stream id.
/// </summary>
/// <param name="template">The stream id template.</param>
/// <param name="property">The missing part.</param>
public class EventStreamIdTemplatePartMissing(string template, string property)
    : Exception($"Event stream id template '{template}' requires a nonblank '{property}' and must not resolve to the default stream id.");
