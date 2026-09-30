// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// Represents an <see cref="IHostedService"/> that logs, when the host starts, the project assemblies that could not
/// be loaded while registering generated metadata.
/// </summary>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> to log with.</param>
/// <remarks>
/// Registered by <c>AddCratisArcCore</c>, so every host that adds Arc reports them, however it was built.
/// </remarks>
internal sealed class SkippedProjectAssembliesReporter(ILogger<GeneratedMetadataRegistration> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        GeneratedMetadataRegistration.Default.LogSkipped(logger);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
