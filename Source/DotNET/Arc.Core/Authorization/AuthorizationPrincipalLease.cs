// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Defers disposal of a principal selected after filters until the whole pipeline execution completes.
/// </summary>
internal sealed class AuthorizationPrincipalLease : IDisposable
{
    IDisposable? _scope;

    /// <inheritdoc/>
    public void Dispose() => _scope?.Dispose();

    /// <summary>
    /// Attaches the authorized execution scope.
    /// </summary>
    /// <param name="scope">The identity scope. Each lease has a single attachment; callers must use a separate lease for another identity.</param>
    internal void Attach(IDisposable scope) => _scope = scope;
}
