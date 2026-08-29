// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Describes the causation recorded for the command a set of events was appended by.
/// </summary>
/// <remarks>
/// The causation chain already says an HTTP request came in; it does not say what that request asked for. This is
/// the link that names it, so an event carries which command produced it - and, when one command executes another,
/// which command produced that one.
/// </remarks>
public static class CommandCausation
{
    /// <summary>
    /// The causation property carrying the name of the command type.
    /// </summary>
    public const string CommandTypeProperty = "commandType";

    /// <summary>
    /// The causation property carrying the fully qualified name of the command type.
    /// </summary>
    public const string CommandTypeFullNameProperty = "commandTypeFullName";

    /// <summary>
    /// The causation property carrying the event sequence an event was appended to.
    /// </summary>
    public const string EventSequenceIdProperty = "eventSequenceId";

    /// <summary>
    /// The <see cref="CausationType"/> recorded for a command.
    /// </summary>
    public static readonly CausationType Type = "Command";

    /// <summary>
    /// Gets the causation properties naming a command.
    /// </summary>
    /// <param name="commandType">The <see cref="System.Type"/> of the command.</param>
    /// <returns>The properties to record on the causation.</returns>
    /// <remarks>
    /// Both the short and the fully qualified name are recorded. The short name is what a person reads and what
    /// behavior is mined by; the fully qualified one is what tells two commands of the same name in different
    /// features apart.
    /// </remarks>
    public static IDictionary<string, string> PropertiesFor(Type commandType) => new Dictionary<string, string>
    {
        { CommandTypeProperty, commandType.Name },
        { CommandTypeFullNameProperty, commandType.FullName ?? commandType.Name }
    };
}
