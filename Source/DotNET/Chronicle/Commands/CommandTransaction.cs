// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Holds the ambient <see cref="IUnitOfWork"/> owned by the command currently executing. Set by
/// <see cref="TransactionalCommandScope"/> when a command begins its transaction and observed by
/// <see cref="TransactionalEventLog"/> to enroll appends. Deliberately separate from Chronicle's own ambient
/// current unit of work, so a unit of work established by other integrations — for example Chronicle's
/// request-level middleware — is left untouched and commands always own their own transaction.
/// </summary>
internal static class CommandTransaction
{
    static readonly AsyncLocal<IUnitOfWork?> _current = new();

    /// <summary>
    /// Gets or sets the unit of work for the command currently executing in this async flow.
    /// </summary>
    internal static IUnitOfWork? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets the command's unit of work when one is active. A completed unit of work — for example when code appends
    /// from a background continuation after its command finished — is not returned.
    /// </summary>
    /// <param name="unitOfWork">The active <see cref="IUnitOfWork"/> when the command's transaction is active.</param>
    /// <returns>True when the command's transaction is active; otherwise false.</returns>
    internal static bool TryGetActive([NotNullWhen(true)] out IUnitOfWork? unitOfWork)
    {
        if (_current.Value is { IsCompleted: false } current)
        {
            unitOfWork = current;
            return true;
        }

        unitOfWork = null;
        return false;
    }
}
