// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Logs MongoDB driver command events, by default as metadata only so document and filter values stay out of the log.
/// </summary>
/// <param name="logger"><see cref="ILogger"/> to log to.</param>
/// <param name="logCommandBodies">Whether to log full command bodies and failure messages.</param>
internal sealed class MongoDBCommandLogging(ILogger<MongoDBClientFactory> logger, bool logCommandBodies)
{
    /// <summary>
    /// Gets whether the logging handlers should be subscribed at all.
    /// </summary>
    internal bool IsActive => logger.IsEnabled(LogLevel.Trace);

    /// <summary>
    /// Subscribes the logging handlers to the cluster builder when logging is enabled.
    /// </summary>
    /// <param name="builder">The <see cref="ClusterBuilder"/> to subscribe on.</param>
    /// <returns>The builder.</returns>
    internal ClusterBuilder Subscribe(ClusterBuilder builder)
    {
        if (!IsActive)
        {
            return builder;
        }

        return builder
            .Subscribe<CommandStartedEvent>(CommandStarted)
            .Subscribe<CommandFailedEvent>(CommandFailed)
            .Subscribe<CommandSucceededEvent>(CommandSucceeded);
    }

    /// <summary>
    /// Logs a started command.
    /// </summary>
    /// <param name="command">The driver event.</param>
    internal void CommandStarted(CommandStartedEvent command)
    {
        if (logCommandBodies)
        {
            logger.CommandStartedWithBody(command.RequestId, command.CommandName, command.Command.ToJson());
            return;
        }

        logger.CommandStarted(command.RequestId, command.CommandName, command.DatabaseNamespace.DatabaseName, GetCollectionName(command));
    }

    /// <summary>
    /// Logs a failed command.
    /// </summary>
    /// <param name="command">The driver event.</param>
    internal void CommandFailed(CommandFailedEvent command)
    {
        if (logCommandBodies)
        {
            logger.CommandFailedWithMessage(command.RequestId, command.CommandName, command.Failure.Message);
            return;
        }

        var (code, codeName) = GetServerError(command.Failure);
        logger.CommandFailed(command.RequestId, command.CommandName, command.Failure.GetType().Name, code, codeName);
    }

    /// <summary>
    /// Logs a succeeded command.
    /// </summary>
    /// <param name="command">The driver event.</param>
    internal void CommandSucceeded(CommandSucceededEvent command) =>
        logger.CommandSucceeded(command.RequestId, command.CommandName, command.Duration.TotalMilliseconds);

    static string? GetCollectionName(CommandStartedEvent command)
    {
        var first = command.Command.ElementCount > 0 ? command.Command.GetElement(0) : default;
        return first.Name == command.CommandName && first.Value is BsonString name ? name.Value : null;
    }

    static (int? Code, string? CodeName) GetServerError(Exception failure) => failure switch
    {
        MongoCommandException commandException => (commandException.Code, commandException.CodeName),
        MongoWriteException writeException => (writeException.WriteError.Code, writeException.WriteError.Category.ToString()),
        _ => (null, null)
    };
}
