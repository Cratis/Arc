// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Validates the authorization catalog in the host's pre-start phase, before any web listener starts.
/// </summary>
/// <param name="validator">The catalog validator.</param>
public class AuthorizationStartupValidation(AuthorizationConfigurationValidator validator) : IHostedLifecycleService
{
    /// <inheritdoc/>
    public Task StartingAsync(CancellationToken cancellationToken) => validator.Validate(cancellationToken);

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
