// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Log messages for <see cref="StaticFilesMiddleware"/>.
/// </summary>
internal static partial class StaticFilesMiddlewareLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Attempting to serve static file for {RequestPath}")]
    internal static partial void AttemptingStaticFile(this ILogger<StaticFilesMiddleware> logger, string requestPath);

    [LoggerMessage(LogLevel.Debug, "Checking {ConfigCount} static file configurations")]
    internal static partial void CheckingConfigurations(this ILogger<StaticFilesMiddleware> logger, int configCount);

    [LoggerMessage(LogLevel.Debug, "Serving static file: {FilePath}")]
    internal static partial void ServingStaticFile(this ILogger<StaticFilesMiddleware> logger, string filePath);

    [LoggerMessage(LogLevel.Debug, "Static file not found: {FilePath} (directory exists: {DirectoryExists}, file exists: {FileExists})")]
    internal static partial void StaticFileNotFound(this ILogger<StaticFilesMiddleware> logger, string filePath, bool directoryExists, bool fileExists);
}
