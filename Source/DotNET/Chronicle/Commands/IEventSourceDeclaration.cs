// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command declaration that appends through an event source definition.
/// </summary>
public interface IEventSourceDeclaration
{
    /// <summary>
    /// Gets the type of the event source definition.
    /// </summary>
    Type EventSource { get; }

    /// <summary>
    /// Gets the optional declared stream.
    /// </summary>
    string? Stream { get; }
}
