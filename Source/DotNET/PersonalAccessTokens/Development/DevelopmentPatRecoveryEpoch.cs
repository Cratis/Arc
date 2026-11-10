// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.PersonalAccessTokens.Development;

/// <summary>
/// Development-only recovery evidence for the lifetime of a process-local authority.
/// No records survive restart, and this is not a durable recovery fence or a production restore strategy.
/// Register with <see cref="DevelopmentPatArcBuilderExtensions.UseDevelopmentPersonalAccessTokenAdapters"/>.
/// </summary>
public sealed class DevelopmentPatRecoveryEpoch : IPatRecoveryEpoch
{
    readonly IHostEnvironment _environment;

    internal DevelopmentPatRecoveryEpoch(IHostEnvironment environment) => _environment = environment;

    /// <inheritdoc/>
    public Task<ulong?> Read(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<ulong?>(_environment.IsDevelopment() ? 1UL : null);
    }
}
