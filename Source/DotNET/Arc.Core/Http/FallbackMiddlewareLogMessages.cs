// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Log messages for <see cref="FallbackMiddleware"/>.
/// </summary>
internal static partial class FallbackMiddlewareLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Serving fallback file: {FilePath}")]
    internal static partial void ServingFallbackFile(this ILogger<FallbackMiddleware> logger, string filePath);
}
