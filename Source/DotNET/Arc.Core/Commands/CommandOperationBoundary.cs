// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using OneOf;

namespace Cratis.Arc.Commands;

/// <summary>
/// Guards integration-owned early commits inside command operation boundaries. Arbitrary user service writes are outside this contract.
/// </summary>
public static class CommandOperationBoundary
{
    static readonly AsyncLocal<CommandOperationFrame?> _current = new();

    /// <summary>
    /// Gets whether the current command can declare operations.
    /// </summary>
    public static bool IsActive => _current.Value?.MayParticipate == true;

    /// <summary>
    /// Rejects an explicit commit before business work when command operations may participate.
    /// </summary>
    /// <exception cref="InvalidCommandOperation">The boundary requires a deferred coordinated commit.</exception>
    public static void ThrowIfActive()
    {
        if (IsActive)
        {
            throw new InvalidCommandOperation("Explicit commits are not supported inside command operation boundaries. Return events for deferred command completion instead.");
        }
    }

    /// <summary>
    /// Enters a command frame, rejecting unsupported nesting before child business execution.
    /// </summary>
    /// <param name="commandType">The command declaration.</param>
    /// <param name="host">The originating Arc host's service scope factory.</param>
    /// <returns>The frame to restore on completion.</returns>
    /// <exception cref="InvalidCommandOperation">Nested operation participation is unsupported.</exception>
    internal static CommandOperationFrame Enter(Type commandType, object host)
    {
        var parent = _current.Value;
        var mayParticipate = commandType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(method => method.Name == "Handle" && MayContainOperations(method.ReturnType));

        // In-process transports can invoke another Arc host (for example Chronicle's kernel). That host owns an
        // independent command boundary, just as it would across HTTP; it must not join this host's operation journal.
        if (parent is not null && ReferenceEquals(parent.Host, host))
        {
            parent.RecordNestedCommand();
            if (parent.MayParticipate || mayParticipate)
            {
                throw new InvalidCommandOperation("Nested commands that may participate in command operations are unsupported. The child has not executed.");
            }
        }

        var frame = new CommandOperationFrame(parent, host, mayParticipate);
        _current.Value = frame;

        return frame;
    }

    /// <summary>
    /// Restores the enclosing command frame after recovery and completion.
    /// </summary>
    /// <param name="frame">The completed frame.</param>
    internal static void Leave(CommandOperationFrame frame) => _current.Value = frame.Parent;

    /// <summary>
    /// Detects operation collections without enumerating arbitrary client response sequences.
    /// </summary>
    /// <param name="type">The runtime response type.</param>
    /// <returns>Whether an explicit batch is required.</returns>
    internal static bool IsBareCollection(Type type) => type != typeof(CommandOperations) &&
        type.GetInterfaces().Append(type).Any(contract => contract.IsGenericType &&
            contract.GetGenericTypeDefinition() == typeof(IEnumerable<>) && MayContainOperations(contract.GetGenericArguments()[0]));

    static bool MayContainOperations(Type type)
    {
        if (type == typeof(CommandOperations) || typeof(ICommandOperation).IsAssignableFrom(type))
        {
            return true;
        }

        if (type.IsGenericType && (type.Namespace == "System.Threading.Tasks" || type.FullName!.StartsWith("System.ValueTuple", StringComparison.Ordinal) ||
            typeof(IOneOf).IsAssignableFrom(type) || type.GetGenericTypeDefinition() == typeof(Nullable<>)))
        {
            return type.GetGenericArguments().Any(MayContainOperations);
        }

        return false;
    }
}
