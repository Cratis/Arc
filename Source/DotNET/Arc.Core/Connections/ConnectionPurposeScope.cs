// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the ambient <see cref="ConnectionPurpose"/> for connections opened on the current execution flow.
/// </summary>
/// <remarks>
/// Connections are opened for <see cref="ConnectionPurpose.Runtime"/> unless a scope says otherwise. Wrap schema changes
/// to have them resolved for <see cref="ConnectionPurpose.Schema"/>:
/// <code>
/// using (ConnectionPurposeScope.Begin(ConnectionPurpose.Schema))
/// {
///     await dbContext.Database.MigrateAsync();
/// }
/// </code>
/// </remarks>
public static class ConnectionPurposeScope
{
    static readonly AsyncLocal<ConnectionPurpose?> _current = new();

    /// <summary>
    /// Gets the <see cref="ConnectionPurpose"/> for the current execution flow.
    /// </summary>
    public static ConnectionPurpose Current => _current.Value ?? ConnectionPurpose.Runtime;

    /// <summary>
    /// Begin a scope where connections are opened for a purpose.
    /// </summary>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous purpose when disposed.</returns>
    public static IDisposable Begin(ConnectionPurpose purpose)
    {
        var previous = _current.Value;
        _current.Value = purpose;
        return new Scope(previous);
    }

    sealed class Scope(ConnectionPurpose? previous) : IDisposable
    {
        public void Dispose() => _current.Value = previous;
    }
}
