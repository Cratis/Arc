// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp.for_McpServerToolsConfiguration.given;

/// <summary>
/// A logger counting the warnings written to it.
/// </summary>
internal sealed class WarningRecorder : ILogger<McpServerToolsConfiguration>
{
    /// <summary>
    /// Gets the number of warnings written.
    /// </summary>
    public int Warnings { get; private set; }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning)
        {
            Warnings++;
        }
    }
}
