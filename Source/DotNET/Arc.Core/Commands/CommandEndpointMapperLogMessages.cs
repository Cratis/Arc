// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

/// <summary>
/// Log messages for <see cref="CommandEndpointMapper"/>.
/// </summary>
internal static partial class CommandEndpointMapperLogMessages
{
    [LoggerMessage(LogLevel.Debug, "The request body for command '{CommandType}' could not be read or deserialized")]
    internal static partial void FailedToReadCommandBody(this ILogger logger, string commandType, Exception? exception);
}
