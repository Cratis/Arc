// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Log messages for <see cref="EventSourceValuesProvider"/>.
/// </summary>
internal static partial class EventSourceValuesProviderLogMessages
{
    [LoggerMessage(LogLevel.Debug, "The event source id provided by command '{CommandType}' could not be composed and was resolved as unspecified")]
    internal static partial void CouldNotComposeProvidedEventSourceId(this ILogger logger, string commandType, Exception? exception);
}
