// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http;

/// <summary>
/// Log messages for <see cref="HttpRequestPipeline"/>.
/// </summary>
internal static partial class HttpRequestPipelineLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Request {Path} was not handled by any middleware, returning 404")]
    internal static partial void RequestNotHandled(this ILogger<HttpRequestPipeline> logger, string path);
}
