// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a stream id template contains malformed braces.
/// </summary>
/// <param name="template">The malformed template.</param>
public class InvalidEventStreamIdTemplate(string template)
    : Exception($"Event stream id template '{template}' contains malformed braces. Use '{{Name}}' for a property and doubled braces for literals.");
